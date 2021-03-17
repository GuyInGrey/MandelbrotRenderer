using System;
using System.Drawing;
using SixLaborsRgba32 = SixLabors.ImageSharp.PixelFormats.Rgba32;

using ComputeSharp;

using SixLabors.ImageSharp;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace MandelbrotRenderer
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting...");

            var dir = @"C:\RenderingTemp\";
            if (Directory.Exists(dir)) { Directory.Delete(dir, true); }
            Directory.CreateDirectory(dir);

            var count = 120;
            Mandelbrot.RenderBatch(count, i =>
            {
                return new RenderProperties()
                {
                    Viewport = Viewport.FromValues(-2.25f, 0.75f, -1.5f, 1.5f),
                    FileName = Path.Combine(dir, $"{i:000000}.png"),
                    ImageSize = (2000, 2000),
                    MaxIterations = 75,
                    Power = MandelbrotShader.Map(i, 0, count - 1, 0, 3),
                };
            }, i =>
            {
                Console.WriteLine($"{i} / {count}");
            });

            var file = Helper.CompileIntoVideo("%06d.png", dir, "output.mp4", 60);
            Helper.OpenWithDefaultProgram(file);

            Console.WriteLine("Done!");
            Console.Read();
        }
    }
}
