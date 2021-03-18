using ComputeSharp;

namespace MandelbrotRenderer
{
    public struct A
    {
        public float B;

        public static A Boi()
        {
            var toReturn = new A();
            toReturn.B = 1;
            return toReturn;
        }
    }
}
