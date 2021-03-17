using System;

using ComputeSharp;

namespace MandelbrotRenderer
{
    public class RenderProperties
    {
        public Viewport Viewport;
        public (int width, int height) ImageSize;
        public float Power;
        public int MaxIterations;
        public string FileName;
    }
}
