using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using ComputeSharp;
using ComputeSharp.SwapChain.Backend;

namespace MandelbrotRenderer
{
    class Program
    {
        static ReadOnlyBuffer<Color> colors;
        static float size = 300;

        static void Main()
        {
            Console.WriteLine("Starting...");

            colors = Gpu.Default.AllocateReadOnlyBuffer(new[]
            {
                Color.FromRGB(0, 0, 0),
                Color.FromRGB(.392f, .584f, .929f),
                Color.FromRGB(1, 1, 1),
            });

            var app = new SwapChainApplication<MandelbrotShader>(static (texture, time) =>
            {
                var pow = (float)(-Math.Cos(time.TotalSeconds / 6f) + 1f) * 1f;



                //size *= 1.01f;
                //Console.WriteLine(size);
                return new MandelbrotShader(texture, 
                    Helper.SizeToViewport(new Float3(-1.2425401f, 0.4132381f, size), texture.Width, texture.Height), 
                    50, pow, colors);
            });
            Thread.Sleep(1000);
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
