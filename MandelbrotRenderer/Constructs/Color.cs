using ComputeSharp;

namespace MandelbrotRenderer
{
    public struct Color
    {
        public float R;
        public float G;
        public float B;

        public static Color FromRGB(float r, float g, float b)
        {
            Color c;
            c.R = r;
            c.G = g;
            c.B = b;
            return c;
        }

        public static Float4 ToFloat4(Color c) =>
            new(c.R, c.G, c.B, 1f);

        public static Color Lerp(Color a, Color b, float t)
        {
            t = Hlsl.Clamp(t, 0, 1);
            Color c;
            c.R = Hlsl.Lerp(a.R, b.R, t);
            c.G = Hlsl.Lerp(a.G, b.G, t);
            c.B = Hlsl.Lerp(a.B, b.B, t);
            return c;
        }
    }
}
