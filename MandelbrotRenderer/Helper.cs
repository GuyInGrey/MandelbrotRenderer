using System;
using System.Diagnostics;
using System.IO;

namespace MandelbrotRenderer
{
    public static class Helper
    {
        public static string CompileIntoVideo(string imageFileNameFormat, string dir, string outputFileName, int fps)
        {
            var pInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = $" -framerate {fps} -i {imageFileNameFormat} -c:v libx264 -r {fps} {outputFileName}",
                WorkingDirectory = dir,
            };
            var p = Process.Start(pInfo);
            p.WaitForExit();
            return Path.Combine(dir, outputFileName);
        }

        public static void OpenWithDefaultProgram(string path)
        {
            var fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }
    }
}
