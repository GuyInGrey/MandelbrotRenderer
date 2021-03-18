using System;

using System.IO;

using ComputeSharp;
using ComputeSharp.SwapChain.Backend;

namespace MandelbrotRenderer
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting...");

            var app = new SwapChainApplication<MandelbrotShader>(static (texture, time) =>
            {
                var pow = (float)(Math.Cos(time.TotalSeconds / 2f) + 2f) * 2f;

                using var colors = Gpu.Default.AllocateReadOnlyBuffer<Color>(new[]
                {
                    Color.FromRGB(255, 0, 0),
                    Color.FromRGB(0, 255, 0),
                    Color.FromRGB(0, 0, 255),
                });

                return new MandelbrotShader(texture, 
                    Helper.SizeToSides(new Float3(0f, 0f, 300f), texture.Width, texture.Height), 
                    50, pow, colors);
            });
            Win32ApplicationRunner.Run(app);
        }

        public static void RenderVideo()
        {
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
                    FileName = Path.Combine(dir, $"{i:000000}.bmp"),
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
                        $"{estimatedMSLeft / 1000:0.0} seconds remaining.");
                }
            });

            Console.WriteLine($"Done rendering! Took {(HighResolutionDateTime.UtcNow - startTime).TotalSeconds} seconds.");
            var video = Helper.CompileIntoVideo("%06d.bmp", dir, "output.mp4", 60);
            Helper.OpenWithDefaultProgram(video);

            Console.WriteLine($"\nDone with everything! Took {(HighResolutionDateTime.UtcNow - startTime).TotalSeconds} seconds.");
        }
    }
}
