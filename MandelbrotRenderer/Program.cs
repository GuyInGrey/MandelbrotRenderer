using System;
using System.Drawing;
using System.Drawing.Imaging;

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
            var bitmap = new Bitmap("blank.png");
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            var viewport = new Float4(-2.25f, 0.75f, -1.5f, 1.5f);
            var pow = 2f;
            var maxIterations = 50;

            try
            {
                var bitmapSpan = new Span<Rgba32>((Rgba32*)bitmapData.Scan0, bitmapData.Width * bitmapData.Height);
                using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(bitmapSpan, bitmap.Width, bitmap.Height);
                var instance = new MandelbrotShader(texture, viewport, maxIterations, pow);
                Gpu.Default.For(bitmap.Width, bitmap.Height, instance);
                texture.CopyTo(bitmapSpan);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            bitmap.Save("Test.png", ImageFormat.Png);
        }
    }
}
