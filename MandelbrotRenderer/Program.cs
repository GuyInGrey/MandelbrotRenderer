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

            var startTime = HighResolutionDateTime.UtcNow;

            var count = 3600;
            var completed = 0;
            var time = HighResolutionDateTime.UtcNow;
            Mandelbrot.RenderBatch(count, i =>
            {
                return new RenderProperties()
                {
                    Viewport = Viewport.FromValues(-2.25f, 0.75f, -1.5f, 1.5f),
                    FileName = Path.Combine(dir, $"{i:000000}.png"),
                    ImageSize = (2000, 2000),
                    MaxIterations = 75,
                    Power = MandelbrotShader.Map(i, 0, count - 1, 0, 6),
                };
            }, i =>
            {
                completed++;
                if (time.AddSeconds(1) < HighResolutionDateTime.UtcNow)
                {
                    time = HighResolutionDateTime.UtcNow;
                    var MSPerFrame = (HighResolutionDateTime.UtcNow - startTime).TotalMilliseconds / (float)completed;
                    var estimatedMSLeft = (count - completed) * MSPerFrame;

                    Console.Clear();
                    Console.WriteLine($"{(completed / (float)count) * 100:0.00}% {completed} / {count}\n" +
                        $"{estimatedMSLeft/1000:0.0} seconds remaining.");
                }
            });

            var video = Helper.CompileIntoVideo("%06d.png", dir, "output.mp4", 60);
            Helper.OpenWithDefaultProgram(video);

            Console.Clear();
            Console.WriteLine("Done!");
            Console.Read();
        }
    }
}
