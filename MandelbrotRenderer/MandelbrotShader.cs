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
            var c = Complex.FromValue(
                Map(ThreadIds.X, 0, image.Width, viewport.X, viewport.Y),
                Map(ThreadIds.Y, 0, image.Height, viewport.Z, viewport.W));

            var z = Complex.Zero();
            int i;
            for (i = 0; i < maxIterations && Complex.Abs(z) <= 2; i++)
            {
                z = Complex.Add(Complex.Pow(z, Complex.FromValue(power, 0)), c);
            }



            float final;
            if (i == maxIterations) { final = i; }
            else
            {
                final = i + 1 - Hlsl.Log(Hlsl.Log(Complex.Abs(z))) / Hlsl.Log(2);
            }
            final /= maxIterations;

            image[ThreadIds.XY] = new Float4((Float3)final, 1f);
        }

        public static float Lerp(float a, float b, float t) =>
            a * (1 - t) + b * t;
        public static float Map(float val, float a1, float b1, float a2, float b2) =>
            Lerp(a2, b2, (val - a1) / (b1 - a1));
    }
}
