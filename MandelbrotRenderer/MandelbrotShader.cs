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
                Map(ThreadIds.X, 0, image.Width, viewport.X, viewport.Y),
                Map(ThreadIds.Y, 0, image.Height, viewport.Z, viewport.W));

            var z = Complex.Zero();
            int i;
            for (i = 0; i < maxIterations && Complex.Abs(z) <= 2; i++)
            {
                z = Complex.Add(Complex.Pow(z, Complex.FromValue(power, 0)), c);
            }

            var t = i == maxIterations ? maxIterations : i + 1 - Hlsl.Log(Hlsl.Log(Complex.Abs(z))) / Hlsl.Log(2);
            t /= maxIterations;

            var index = (int)(colors.Length * t);
            var nextIndex = (index + 1) % colors.Length;
            var worth = 1f / colors.Length;
            var r = (t - (worth * index)) / worth;
            var color = Color.Lerp(colors[index], colors[nextIndex], r);

            if (i == maxIterations)
            {
                color = Color.FromRGB(0, 0, 0);
            }
            image[ThreadIds.XY] = Color.ToFloat4(color);
        }

        public static float Lerp(float a, float b, float t) =>
            a * (1 - t) + b * t;
        public static float Map(float val, float a1, float b1, float a2, float b2) =>
            Lerp(a2, b2, (val - a1) / (b1 - a1));
    }

    public struct Complex
    {
        public float Real; // X
        public float Imaginary; // Y

        public static Complex Zero() => FromValue(0, 0);
        public static Complex One() => FromValue(1, 0);

        public static Complex FromValue(float r, float i)
        {
            Complex toReturn;
            toReturn.Real = r;
            toReturn.Imaginary = i;
            return toReturn;
        }

        public static Complex Add(Complex a, Complex b)
        {
            Complex toReturn;
            toReturn.Real = a.Real + b.Real;
            toReturn.Imaginary = a.Imaginary + b.Imaginary;
            return toReturn;
        }

        public static float Abs(Complex value) =>
            Hlsl.Sqrt(value.Real * value.Real + value.Imaginary * value.Imaginary);

        public static Complex Pow(Complex value, Complex power)
        {
            if (power.Real == 0 && power.Imaginary == 0) { return Complex.One(); }
            if (value.Real == 0 && value.Imaginary == 0) { return Complex.Zero(); }

            var a = value.Real;
            var b = value.Imaginary;
            var c = power.Real;
            var d = power.Imaginary;

            var rho = Complex.Abs(value);
            var theta = Hlsl.Atan2(b, a);
            var newRho = c * theta + d * Hlsl.Log(rho);

            var t = Hlsl.Pow(rho, c) * Hlsl.Pow(2.718282f, -d * theta);
            return FromValue(t * Hlsl.Cos(newRho), t * Hlsl.Sin(newRho));
        }
    }

    public struct Color
    {
        public float R;
        public float G;
        public float B;

        public static Color FromRGB(float r, float g, float b)
        {
            Color c;
            c.R = r;
            c.G = g;
            c.B = b;
            return c;
        }

        public static Float4 ToFloat4(Color c) =>
            new(c.R, c.G, c.B, 1f);

        public static Color Lerp(Color a, Color b, float t)
        {
            t = Hlsl.Clamp(t, 0, 1);
            Color c;
            c.R = Hlsl.Lerp(t, a.R, b.R);
            c.G = Hlsl.Lerp(t, a.G, b.G);
            c.B = Hlsl.Lerp(t, a.B, b.B);
            return c;
        }
    }
}
