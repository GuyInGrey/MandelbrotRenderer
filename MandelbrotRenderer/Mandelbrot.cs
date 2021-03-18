using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ComputeSharp;

using SixLabors.ImageSharp;
using SixLaborsRgba32 = SixLabors.ImageSharp.PixelFormats.Rgba32;

namespace MandelbrotRenderer
{
    public static class Mandelbrot
    {
        public static void RenderAndSave(RenderProperties props)
        {
            using Image<SixLaborsRgba32> image = new(props.ImageSize.width, props.ImageSize.height);
            using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(image.Width, image.Height);
            using var colors = Gpu.Default.AllocateReadOnlyBuffer<Color>(new []
            {
                Color.FromRGB(255, 0, 0),
                Color.FromRGB(0, 255, 0),
                Color.FromRGB(0, 0, 255),
            });

            var instance = new MandelbrotShader(texture, 
                Helper.SizeToViewport(new Float3(0f, 0f, 240f), image.Width, image.Height), 
                props.MaxIterations, props.Power, colors);
            Gpu.Default.For(texture.Width, texture.Height, instance);
            _ = image.TryGetSinglePixelSpan(out var span);
            texture.CopyTo(MemoryMarshal.Cast<SixLaborsRgba32, Rgba32>(span));
            image.SaveAsPng(props.FileName);

            image.Dispose();
            texture.Dispose();
        }

        public static void RenderBatch(int count, Func<int, RenderProperties> getProps, Action<int> onFrameFinish = null)
        {
            if (getProps is null) { throw new InvalidOperationException("getProps must not be null"); }
            var defaultProps = getProps(0);

            Parallel.For(0, count, i =>
            {
                using Image<SixLaborsRgba32> image = new(defaultProps.ImageSize.width, defaultProps.ImageSize.height);
                using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(image.Width, image.Height);
                using var buffer = Gpu.Default.AllocateReadWriteBuffer<int>(image.Width * image.Height);
                using var colors = Gpu.Default.AllocateReadOnlyBuffer<Color>(new[]
                {
                    Color.FromRGB(255, 0, 0),
                    Color.FromRGB(0, 255, 0),
                    Color.FromRGB(0, 0, 255),
                });

                var props = getProps(i);

                var instance = new MandelbrotShader(texture, 
                    Helper.SizeToViewport(new Float3(0f, 0f, 240f), image.Width, image.Height), 
                    props.MaxIterations, props.Power, colors);
                Gpu.Default.For(texture.Width, texture.Height, instance);
                _ = image.TryGetSinglePixelSpan(out var span);
                texture.CopyTo(MemoryMarshal.Cast<SixLaborsRgba32, Rgba32>(span));
                image.SaveAsBmp(props.FileName);
                onFrameFinish?.Invoke(i);

                image.Dispose();
                texture.Dispose();
                buffer.Dispose();
            });
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
            RenderBatch(count, i =>
            {
                return new RenderProperties()
                {
                    Viewport = Viewport.FromValues(-2.25f, 0.75f, -1.5f, 1.5f),
                    FileName = Path.Combine(dir, $"{i:000000}.bmp"),
                    ImageSize = (2000, 2000),
                    MaxIterations = 75,
                    Power = HlslHelper.Map(i, 0, count - 1, 0, 6),
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
