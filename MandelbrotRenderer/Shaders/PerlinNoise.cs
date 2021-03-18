using ComputeSharp;

namespace MandelbrotRenderer
{
    public readonly partial struct PerlinNoise : IComputeShader
    {
        public readonly int Octaves;
        public readonly float Z;
        public readonly ReadOnlyBuffer<int> PerlinPermutation;
        public readonly int HalfLength;
        public readonly Float4 Viewport;
        public readonly int Width;
        public readonly int Height;
        public readonly ReadWriteBuffer<float> Results;
        public readonly ReadWriteBuffer<float> MinMax;

        public void Execute()
        {
            var pXY = new Float3(
                HlslHelper.Map(ThreadIds.X, 0, Width, Viewport.X, Viewport.Y),
                HlslHelper.Map(ThreadIds.Y, 0, Height, Viewport.Z, Viewport.W), 
                Z
            );

            var r = NoisePosO(pXY, 5);
            Results[ThreadIds.X + ThreadIds.Y * Width] = r;
            MinMax[0] = Hlsl.Min(MinMax[0], r);
            MinMax[1] = Hlsl.Max(MinMax[1], r);
        }

        static float Fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }

        static float Grad(int hash, float x, float y, float z)
        {
            var h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE
            float u = h < 8 ? x : y,                 // INTO 12 GRADIENT DIRECTIONS.
                   v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public float NoiseXYZ(float x, float y, float z)
        {
            var X = (int)Hlsl.Floor(x) % HalfLength;
            var Y = (int)Hlsl.Floor(y) % HalfLength;
            var Z = (int)Hlsl.Floor(z) % HalfLength;

            if (X < 0)
            {
                X += HalfLength;
            }

            if (Y < 0)
            {
                Y += HalfLength;
            }

            if (Z < 0)
            {
                Z += HalfLength;
            }

            x -= (int)Hlsl.Floor(x);
            y -= (int)Hlsl.Floor(y);
            z -= (int)Hlsl.Floor(z);

            var u = Fade(x);
            var v = Fade(y);
            var w = Fade(z);

            int A = PerlinPermutation[X] + Y, AA = PerlinPermutation[A] + Z, AB = PerlinPermutation[A + 1] + Z,      // HASH COORDINATES OF
                B = PerlinPermutation[X + 1] + Y, BA = PerlinPermutation[B] + Z, BB = PerlinPermutation[B + 1] + Z;      // THE 8 CUBE CORNERS,


            return Hlsl.Lerp(
                    Hlsl.Lerp(
                         Hlsl.Lerp(
                            Grad(PerlinPermutation[AA], x, y, z) // AND ADD
                            ,
                            Grad(PerlinPermutation[BA], x - 1, y, z) // BLENDED
                            ,
                            u
                            )
                        ,
                        Hlsl.Lerp(
                            Grad(PerlinPermutation[AB], x, y - 1, z)  // RESULTS
                            ,
                            Grad(PerlinPermutation[BB], x - 1, y - 1, z)
                            ,
                            u
                            )
                        ,
                        v
                    )
                    ,
                    Hlsl.Lerp(
                        Hlsl.Lerp(
                            Grad(PerlinPermutation[AA + 1], x, y, z - 1) // CORNERS
                            ,
                            Grad(PerlinPermutation[BA + 1], x - 1, y, z - 1) // OF CUBE
                            ,
                            u
                            )
                        ,
                        Hlsl.Lerp(
                            Grad(PerlinPermutation[AB + 1], x, y - 1, z - 1)
                            ,
                            Grad(PerlinPermutation[BB + 1], x - 1, y - 1, z - 1)
                            ,
                            u
                            )
                        ,
                        v
                    )
                    ,
                    w
                );
        }

        public float NoiseXYZO(float x, float y, float z, int octaves)
        {
            var perlin = 0f;
            var octave = 1;

#pragma warning disable IDE0007 // Use implicit type
            for (int i = 0; i < octaves; i++)
#pragma warning restore IDE0007 // Use implicit type
            {
                var noise = NoiseXYZ(x * octave, y * octave, z * octave);

                perlin += noise / octave;

                octave *= 2;
            }

            perlin = Hlsl.Abs((float)Hlsl.Pow(perlin, 2));
            return perlin;
        }

        public float NoisePosO(Float3 position, int octaves)
        {
            return NoiseXYZO(position.X, position.Y, position.Z, octaves);
        }
    }
}
