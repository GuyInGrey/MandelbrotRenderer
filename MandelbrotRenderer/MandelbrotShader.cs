using ComputeSharp;

namespace MandelbrotRenderer
{
    [AutoConstructor]
    public readonly partial struct MandelbrotShader : IComputeShader
    {
        public readonly ReadWriteTexture2D<Rgba32, Float4> image;
        public readonly Float4 viewport;
        public readonly int maxIterations;
        public readonly float power;

        public void Execute()
        {
            var c = new Float2(
                Hlsl.Lerp(ThreadIds.X / (float)image.Width, viewport.X, viewport.Z),
                Hlsl.Lerp(ThreadIds.Y / (float)image.Height, viewport.Y, viewport.W));
            var z = c;

            var i = 0;
            while (ComplexAbs(z) <= 2 && i < maxIterations)
            {
                z = ComplexPow(z, new Float2(power, 0)) + c;
                i++;
            }

            float final;
            if (i == maxIterations) { final = i; }
            else
            {
                final = i + 1 - Hlsl.Log(Hlsl.Log(ComplexAbs(z))) / Hlsl.Log(2);
            }
            final /= maxIterations;

            image[ThreadIds.XY] = new Float4((Float3)final, 1f);
        }

        public static float ComplexAbs(Float2 complex)
        {
            var c = Hlsl.Abs(complex.X);
            var d = Hlsl.Abs(complex.Y);
            if (c > d)
            {
                var r = d / c;
                return c * Hlsl.Sqrt(1f + r * r);
            }
            else if (d == 0)
            {
                return c;
            }
            else
            {
                var r = c / d;
                return d * Hlsl.Sqrt(1f + r * r);
            }
        }

        public static Float2 ComplexPow(Float2 val, Float2 pow)
        {
            if (pow.X == 0 && pow.Y == 0) { return Float2.One; }
            if (val.X == 0 && val.Y == 0) { return Float2.Zero; }

            var rho = ComplexAbs(val);
            var theta = Hlsl.Atan2(val.Y, val.X);
            var newRho = pow.X * theta + pow.Y * Hlsl.Log(rho);
            var t = Hlsl.Pow(rho, pow.X) * Hlsl.Pow(2.71828182845f, -pow.Y * theta);
            return new Float2(t * Hlsl.Cos(newRho), t * Hlsl.Sin(newRho));
        }
    }
}
