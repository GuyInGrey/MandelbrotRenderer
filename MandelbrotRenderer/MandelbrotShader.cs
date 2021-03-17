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

            var final = i == maxIterations ? i : i + 1 - Hlsl.Log(Hlsl.Log(Complex.Abs(z))) / Hlsl.Log(2);
            final /= maxIterations;

            image[ThreadIds.XY] = new Float4((Float3)final, 1f);
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
}
