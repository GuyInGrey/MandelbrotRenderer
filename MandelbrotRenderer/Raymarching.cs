using System;

using ComputeSharp;

namespace MandelbrotRenderer
{
    [AutoConstructor]
    public readonly partial struct Raymarching : IComputeShader
    {
        public readonly IReadWriteTexture2D<Float4> Destination;

        public readonly Float4x4 _CameraToWorld;
        public readonly Float4x4 _CameraInverseProjection;

        public readonly Float3 _Light;
        public readonly bool positionLight;

#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static float maxDst = 80;
        public static float epsilon = 0.001f;
        public static float shadowBias = epsilon * 50;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public struct Shape
        {
            public Float3 position;
            public Float3 size;
            public Float3 colour;
            public int shapeType;
            public int operation;
            public float blendStrength;
            public int numChildren;
        };

        public readonly ReadWriteBuffer<Shape> shapes;
        public readonly int numShapes;

        public struct Ray
        {
            public Float3 origin;
            public Float3 direction;
        };

        public static float SphereDistance(Float3 eye, Float3 centre, float radius)
        {
            return Hlsl.Length(eye - centre) - radius;
        }

        public static float CubeDistance(Float3 eye, Float3 centre, Float3 size)
        {
            var o = Hlsl.Abs(eye - centre) - size;
            var ud = Hlsl.Length(Hlsl.Max(o, 0));
            var n = Hlsl.Max(Hlsl.Max(Hlsl.Min(o.X, 0), Hlsl.Min(o.Y, 0)), Hlsl.Min(o.Z, 0));
            return ud + n;
        }

        // Following distance functions from http://iquilezles.org/www/articles/distfunctions/distfunctions.htm
        public static float TorusDistance(Float3 eye, Float3 centre, float r1, float r2)
        {
            var q = new Float2(Hlsl.Length((eye - centre).XZ) - r1, eye.Y - centre.Y);
            return Hlsl.Length(q) - r2;
        }

        public static float PrismDistance(Float3 eye, Float3 centre, Float3 h)
        {
            var q = Hlsl.Abs(eye - centre);
            return Hlsl.Max(q.Z - h.Y, Hlsl.Max(q.X * 0.866025f + eye.Y * 0.5f, -eye.Y) - h.X * 0.5f);
        }

        public static float CylinderDistance(Float3 eye, Float3 centre, Float2 h)
        {
            var d = Hlsl.Abs(new Float2(Hlsl.Length((eye).XZ), eye.Y)) - h;
            return Hlsl.Length(Hlsl.Max(d, 0.0f)) + Hlsl.Max(Hlsl.Min(d.X, 0), Hlsl.Min(d.Y, 0));
        }

        public static Ray CreateRay(Float3 origin, Float3 direction)
        {
            Ray ray;
            ray.origin = origin;
            ray.direction = direction;
            return ray;
        }

        Ray CreateCameraRay(Float2 uv)
        {
            var origin = Hlsl.Mul(_CameraToWorld, new Float4(0, 0, 0, 1)).XYZ;
            var direction = Hlsl.Mul(_CameraInverseProjection, new Float4(uv, 0, 1)).XYZ;
            direction = Hlsl.Mul(_CameraToWorld, new Float4(direction, 0)).XYZ;
            direction = Hlsl.Normalize(direction);
            return CreateRay(origin, direction);
        }

        // polynomial smooth min (k = 0.1);
        // from https://www.iquilezles.org/www/articles/smin/smin.htm
        public static Float4 Blend(float a, float b, Float3 colA, Float3 colB, float k)
        {
            var h = Hlsl.Clamp(0.5f + 0.5f * (b - a) / k, 0.0f, 1.0f);
            var blendDst = Hlsl.Lerp(b, a, h) - k * h * (1.0f - h);
            var blendCol = Hlsl.Lerp(colB, colA, h);
            return new Float4(blendCol, blendDst);
        }

        public static Float4 Combine(float dstA, float dstB, Float3 colourA, Float3 colourB, int operation, float blendStrength)
        {
            var dst = dstA;
            var colour = colourA;

            if (operation == 0)
            {
                if (dstB < dstA)
                {
                    dst = dstB;
                    colour = colourB;
                }
            }
            // Blend
            else if (operation == 1)
            {
                var blend = Blend(dstA, dstB, colourA, colourB, blendStrength);
                dst = blend.W;
                colour = blend.XYZ;
            }
            // Cut
            else if (operation == 2)
            {
                // Hlsl.Max(a,-b)
                if (-dstB > dst)
                {
                    dst = -dstB;
                    colour = colourB;
                }
            }
            // Mask
            else if (operation == 3)
            {
                // Hlsl.Max(a,b)
                if (dstB > dst)
                {
                    dst = dstB;
                    colour = colourB;
                }
            }

            return new Float4(colour, dst);
        }

        public static float GetShapeDistance(Shape shape, Float3 eye)
        {

            if (shape.shapeType == 0)
            {
                return SphereDistance(eye, shape.position, shape.size.X);
            }
            else if (shape.shapeType == 1)
            {
                return CubeDistance(eye, shape.position, shape.size);
            }
            else if (shape.shapeType == 2)
            {
                return TorusDistance(eye, shape.position, shape.size.X, shape.size.Y);
            }

            return maxDst;
        }


        Float4 SceneInfo(Float3 eye)
        {
            var globalDst = maxDst;
            Float3 globalColour = 1;

            for (int i = 0; i < numShapes; i++)
            {
                var shape = shapes[i];
                var numChildren = shape.numChildren;

                var localDst = GetShapeDistance(shape, eye);
                var localColour = shape.colour;


                for (int j = 0; j < numChildren; j++)
                {
                    var childShape = shapes[i + j + 1];
                    var childDst = GetShapeDistance(childShape, eye);

                    var combined = Combine(localDst, childDst, localColour, childShape.colour, childShape.operation, childShape.blendStrength);
                    localColour = combined.XYZ;
                    localDst = combined.W;
                }
                i += numChildren; // skip over children in outer loop

                var globalCombined = Combine(globalDst, localDst, globalColour, localColour, shape.operation, shape.blendStrength);
                globalColour = globalCombined.XYZ;
                globalDst = globalCombined.W;
            }

            return new Float4(globalColour, globalDst);
        }

        Float3 EstimateNormal(Float3 p)
        {
            var x = SceneInfo(new Float3(p.X + epsilon, p.Y, p.Z)).W - SceneInfo(new Float3(p.X - epsilon, p.Y, p.Z)).W;
            var y = SceneInfo(new Float3(p.X, p.Y + epsilon, p.Z)).W - SceneInfo(new Float3(p.X, p.Y - epsilon, p.Z)).W;
            var z = SceneInfo(new Float3(p.X, p.Y, p.Z + epsilon)).W - SceneInfo(new Float3(p.X, p.Y, p.Z - epsilon)).W;
            return Hlsl.Normalize(new Float3(x, y, z));
        }

        float CalculateShadow(Ray ray, float dstToShadePoint)
        {
            float rayDst = 0;
            var marchSteps = 0;
            var shadowIntensity = .2f;
            float brightness = 1;

            while (rayDst < dstToShadePoint)
            {
                marchSteps++;
                var sceneInfo = SceneInfo(ray.origin);
                var dst = sceneInfo.W;

                if (dst <= epsilon)
                {
                    return shadowIntensity;
                }

                brightness = Hlsl.Min(brightness, dst * 200);

                ray.origin += ray.direction * dst;
                rayDst += dst;
            }
            return shadowIntensity + (1 - shadowIntensity) * brightness;
        }

        public void Execute()
        {
            Destination[ThreadIds.XY] = new Float4(.392f, .584f, .929f, 1);

            var uv = ThreadIds.XY / new Float2(Destination.Width, Destination.Height) * 2 - 1;
            float rayDst = 0;

            var ray = CreateCameraRay(uv);
            var marchSteps = 0;

            while (rayDst < maxDst)
            {
                marchSteps++;
                var sceneInfo = SceneInfo(ray.origin);
                var dst = sceneInfo.W;

                if (dst <= epsilon)
                {
                    var pointOnSurface = ray.origin + ray.direction * dst;
                    var normal = EstimateNormal(pointOnSurface - ray.direction * epsilon);
                    var lightDir = (positionLight) ? Hlsl.Normalize(_Light - ray.origin) : -_Light;
                    var lighting = Hlsl.Saturate(Hlsl.Saturate(Hlsl.Dot(normal, lightDir)));
                    var col = sceneInfo.XYZ;

                    // Shadow
                    var offsetPos = pointOnSurface + normal * shadowBias;
                    var dirToLight = (positionLight) ? Hlsl.Normalize(_Light - offsetPos) : -_Light;

                    ray.origin = offsetPos;
                    ray.direction = dirToLight;

                    var dstToLight = (positionLight) ? Hlsl.Length(offsetPos - _Light) : maxDst;
                    var shadow = CalculateShadow(ray, dstToLight);

                    Destination[ThreadIds.XY] = new Float4(col * lighting * shadow, 1);

                    break;
                }

                ray.origin += ray.direction * dst;
                rayDst += dst;
            }
        }
    }
}
