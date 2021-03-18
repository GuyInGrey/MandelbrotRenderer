using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

using ComputeSharp;
using ComputeSharp.SwapChain.Backend;

namespace MandelbrotRenderer
{
    class Program
    {
        static ReadWriteBuffer<int> perms;

        static void Main()
        {
            Console.WriteLine("Starting...");

            NoiseMaker.Reseed();
            perms = Gpu.Default.AllocateReadWriteBuffer(NoiseMaker.p);

            var app = new SwapChainApplication<Space>(static (tex, time) =>
            {
                using var perlin = Gpu.Default.AllocateReadWriteBuffer<float>(tex.Width * tex.Height);

                var v = Helper.SizeToViewport(new Float3(0, 0, 300), tex.Width, tex.Height);
                return new Space(perms, tex, NoiseMaker._halfLength, v);
            });

            Win32ApplicationRunner.Run(app);
        }
    }
}
