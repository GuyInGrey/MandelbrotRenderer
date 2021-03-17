using ComputeSharp;

namespace MandelbrotRenderer
{
    public struct Viewport
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;

        public static Viewport FromValues(float l, float r, float t, float b)
        {
            Viewport toReturn;
            toReturn.Left = l;
            toReturn.Right = r;
            toReturn.Top = t;
            toReturn.Bottom = b;
            return toReturn;
        }
        
        public static Float4 ToFloat4(Viewport v) =>
            new(v.Left, v.Right, v.Top, v.Bottom);
    }
}
