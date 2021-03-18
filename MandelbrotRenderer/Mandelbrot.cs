using System;
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
                Helper.SizeToSides(new Float3(0f, 0f, 240f), image.Width, image.Height), 
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
                    Helper.SizeToSides(new Float3(0f, 0f, 240f), image.Width, image.Height), 
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
    }
}
