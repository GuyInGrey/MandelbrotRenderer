using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

using ComputeSharp;

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
            var bitmap = new Bitmap(10, 10);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            var viewport = new Float4(-2.25f, 0.75f, -1.5f, 1.5f);
            var pow = 2f;
            var maxIterations = 50;
            var debug = new bool[bitmap.Width * bitmap.Height];
            debug[0] = true;

            try
            {
                var bitmapSpan = new Span<Rgba32>((Rgba32*)bitmapData.Scan0, bitmapData.Width * bitmapData.Height);
                using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(bitmapSpan, bitmap.Width, bitmap.Height);
                using var debugData = Gpu.Default.AllocateReadWriteBuffer(debug);
                var instance = new MandelbrotShader(texture, viewport, maxIterations, pow, debugData);
                Gpu.Default.For(bitmap.Width, bitmap.Height, instance);
                texture.CopyTo(bitmapSpan);
                debugData.CopyTo(debug);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            bitmap.Save("test.png", ImageFormat.Png);
            Console.WriteLine(string.Join("\n", debug));
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
