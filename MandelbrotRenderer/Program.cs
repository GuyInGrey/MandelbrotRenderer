using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

using ComputeSharp;
using ComputeSharp.SwapChain.Backend;

namespace MandelbrotRenderer
{
    class Program
    {
        static ReadOnlyBuffer<Color> colors;
        static ReadWriteBuffer<Raymarching.Shape> shapes;
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

            shapes = Gpu.Default.AllocateReadWriteBuffer<Raymarching.Shape>(2);

            //var app = new SwapChainApplication<MandelbrotShader>(static (texture, time) =>
            //{
            //    var pow = (float)(-Math.Cos(time.TotalSeconds / 6f) + 1f) * 1f;
            //    return new MandelbrotShader(texture, 
            //        Helper.SizeToViewport(new Float3(-1.2425401f, 0.4132381f, size), texture.Width, texture.Height), 
            //        10000, pow, colors);
            //});

            var app = new SwapChainApplication<Raymarching>(static (texture, time) =>
            {
                shapes.CopyFrom(new[]
                {
                    new Raymarching.Shape()
                    {
                        blendStrength = 0.3f,
                        colour = new Float3(0, 1, 0),
                        numChildren = 0,
                        operation = 1,
                        position = Float3.Zero,
                        shapeType = 0,
                        size = 1f,
                    },
                    new Raymarching.Shape()
                    {
                        blendStrength = 0.3f,
                        colour = new Float3(1, 0, 0),
                        numChildren = 0,
                        operation = 1,
                        position = new Float3((float)Math.Cos(time.TotalSeconds) * 3, 0, 0),
                        shapeType = 2,
                        size = (float)Math.Sin(time.TotalMilliseconds / 500) + 1,
                    },
                });

                var light = new Float3(2, 6, 1);

                var camPos = new Float3(0f, 2.1f, -6);

                var cam = new Float4x4(
                    1, 0, 0, camPos.X,
                    0, .93f, .35f, camPos.Y,
                    0, .35f, -.93f, camPos.Z,
                    0, 0, 0, 1);

                var inverseProj = new Float4x4(
                    1.0264f, 0, 0, 0,
                    0, 0.57735f, 0, 0,
                    0, 0, 0, -1,
                    0, 0, -1.66617f, 1.66717f
                );

                return new Raymarching(texture, cam, inverseProj, light, true, shapes, shapes.Length);
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
