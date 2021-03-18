using ComputeSharp;

namespace MandelbrotRenderer
{
    public static class HlslHelper
    {
        public static float Map(float val, float a1, float b1, float a2, float b2) =>
            Hlsl.Lerp(a2, b2, (val - a1) / (b1 - a1));
        public static float Log(float b, float x) =>
            Hlsl.Log(x) / Hlsl.Log(b);
    }
}
