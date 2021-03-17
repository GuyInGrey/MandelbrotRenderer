using System;
using System.Drawing;
using SixLaborsRgba32 = SixLabors.ImageSharp.PixelFormats.Rgba32;

using ComputeSharp;

using SixLabors.ImageSharp;
using System.Runtime.InteropServices;

namespace MandelbrotRenderer
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting...");
            Run();
            Console.WriteLine("Done!");
            Console.Read();
        }

        public static unsafe void Run()
        {
            using Image<SixLaborsRgba32> image = new(4096, 4096);
            using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(image.Width, image.Height);
            using var buffer = Gpu.Default.AllocateReadWriteBuffer<int>(image.Width * image.Height);

            var viewport = new Float4(-2.25f, -1.5f, 0.75f, 1.5f);
            var pow = 2f;
            var maxIterations = 50;

            var instance = new MandelbrotShader(texture, viewport, maxIterations, pow);
            Gpu.Default.For(texture.Width, texture.Height, instance);
            _ = image.TryGetSinglePixelSpan(out var span);
            texture.CopyTo(MemoryMarshal.Cast<SixLaborsRgba32, Rgba32>(span));

            image.SaveAsPng(@"test.png");
        }

        // For testing
        public static float ComplexAbs(float cX, float cY)
        {
            var c = Math.Abs(cX);
            var d = Math.Abs(cY);
            if (c > d)
            {
                var r = d / c;
                return c * (float)Math.Sqrt(1f + r * r);
            }
            else if (d == 0)
            {
                return c;
            }
            else
            {
                var r = c / d;
                return d * (float)Math.Sqrt(1f + r * r);
            }
        }

        // For testing
        public static (float, float) ComplexPow(float valX, float valY, float powX, float powY)
        {
            if (powX == 0 && powY == 0) { return (1, 1); }
            if (valX == 0 && valY == 0) { return (0, 0); }

            var rho = ComplexAbs(valX, valY);
            var theta = Math.Atan2(valY, valX);
            var newRho = powX * theta + powY * Math.Log(rho);
            var t = (float)Math.Pow(rho, powX) * (float)Math.Pow(2.71828182845f, -powY * theta);
            return (t * (float)Math.Cos(newRho), t * (float)Math.Sin(newRho));
        }
    }
}
