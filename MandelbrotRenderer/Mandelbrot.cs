using System;
using System.Runtime.InteropServices;

using ComputeSharp;

using SixLabors.ImageSharp;
using SixLaborsRgba32 = SixLabors.ImageSharp.PixelFormats.Rgba32;

namespace MandelbrotRenderer
{
    public static class Mandelbrot
    {
        public static unsafe void RenderAndSave(RenderProperties props)
        {
            using Image<SixLaborsRgba32> image = new(props.ImageSize.width, props.ImageSize.height);
            using var texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(image.Width, image.Height);
            using var buffer = Gpu.Default.AllocateReadWriteBuffer<int>(image.Width * image.Height);

            var instance = new MandelbrotShader(texture, props.Viewport, props.MaxIterations, props.Power);
            Gpu.Default.For(texture.Width, texture.Height, instance);
            _ = image.TryGetSinglePixelSpan(out var span);
            texture.CopyTo(MemoryMarshal.Cast<SixLaborsRgba32, Rgba32>(span));
            image.SaveAsPng(props.FileName);
        }

        public static void RenderBatch(int count)
        {

        }
    }
}
