using ComputeSharp;

namespace MandelbrotRenderer
{
    [AutoConstructor]
    public readonly partial struct MandelbrotShader : IComputeShader
    {
        public readonly IReadWriteTexture2D<Float4> image;
        public readonly Float4 viewport;
        public readonly int maxIterations;
        public readonly float power;
        public readonly ReadOnlyBuffer<Color> colors;

        public void Execute()
        {
            var c = Complex.FromValue(
                HlslHelper.Map(ThreadIds.X, 0, image.Width, viewport.X, viewport.Y),
                HlslHelper.Map(ThreadIds.Y, 0, image.Height, viewport.Z, viewport.W));

            var z = Complex.Zero();
            int i;
            for (i = 0; i < maxIterations && Complex.Abs(z) <= 2; i++)
            {
                z = Complex.Add(Complex.Pow(z, Complex.FromValue(power, 0)), c);
            }

            var t = i == maxIterations ? maxIterations : i + 1 - Hlsl.Log(Hlsl.Log(Complex.Abs(z))) / Hlsl.Log(power);
            t /= maxIterations;

            t = Hlsl.Clamp(t, 0, 0.999f);

            var index = (int)(colors.Length * t);
            var nextIndex = (index + 1) % colors.Length;
            var worth = 1f / colors.Length;
            var r = (t - (worth * index)) / worth;
            var color = Color.Lerp(colors[index], colors[nextIndex], r);

            if (i == maxIterations)
            {
                color = colors[colors.Length - 1];
            }
            image[ThreadIds.XY] = Color.ToFloat4(color);
        }
    }
}
