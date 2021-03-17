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

            Mandelbrot.RenderAndSave(new RenderProperties()
            {
                Viewport = Viewport.FromValues(0, 0, 0, 0),
                FileName = "test.png",
                ImageSize = (8192, 8192),
                MaxIterations = 75,
                Power = 2f,
            });

            Console.WriteLine("Done!");
            Console.Read();
        }
    }
}
