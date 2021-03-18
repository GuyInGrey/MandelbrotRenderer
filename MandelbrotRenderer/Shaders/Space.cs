using ComputeSharp;

namespace MandelbrotRenderer
{
    [AutoConstructor]
    public partial struct Space : IComputeShader
    {
        public readonly IReadWriteTexture2D<Float4> Image;
        public readonly ReadWriteBuffer<float> Perlin;
        public readonly float PerlinMin;
        public readonly float PerlinMax;

        public void Execute()
        {
            //var color = Color.Lerp(Color.FromRGB(0, 0, 0), Color.FromRGB(1, 1, 1), p);
            //Image[ThreadIds.XY] = Color.ToFloat4(color);
        }
    }
}
