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
            Complex c;
            c.Real = Map(ThreadIds.X, 0, image.Width, viewport.X, viewport.Z);
            c.Imaginary = Map(ThreadIds.Y, 0, image.Height, viewport.Y, viewport.W);

            var z = Complex.Zero();

            var i = 0;
            while (Complex.Abs(z) <= 2 && i < maxIterations)
            {
                Complex pow;
                pow.Real = power;
                pow.Imaginary = 0;
                z = Complex.Add(Complex.Pow(z, pow), c);
                i++;
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

    public struct Complex
    {
        public float Real; // X
        public float Imaginary; // Y

        public static Complex Add(Complex a, Complex b)
        {
            Complex toReturn;
            toReturn.Real = a.Real + b.Real;
            toReturn.Imaginary = a.Imaginary + b.Imaginary;
            return toReturn;
        }

        public static Complex Zero()
        {
            Complex toReturn;
            toReturn.Real = 0;
            toReturn.Imaginary = 0;
            return toReturn;
        }

        public static Complex One()
        {
            Complex toReturn;
            toReturn.Real = 1;
            toReturn.Imaginary = 0;
            return toReturn;
        }

        public static float Abs(Complex value)
        {
            return Hlsl.Sqrt(value.Real * value.Real + value.Imaginary * value.Imaginary);
        }

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
            Complex toReturn;
            toReturn.Real = t * Hlsl.Cos(newRho);
            toReturn.Imaginary = t * Hlsl.Sin(newRho);

            return toReturn;
        }
    }
}
