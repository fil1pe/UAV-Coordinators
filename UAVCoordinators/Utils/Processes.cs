using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UAVCoordinators
{
    internal partial class Utils
    {
        public static string RunPythonProc(string app, string[] args)
        {
            string python = @"C:\Python27\python.exe";
            string arg = app;
            foreach (var i in args) arg += " " + i;

            ProcessStartInfo psi = new ProcessStartInfo(python, arg);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;

            var proc = Process.Start(psi);

            StreamReader sr = proc.StandardOutput;

            string res = "";

            while (!proc.HasExited)
            {
                if (!sr.EndOfStream)
                    res = sr.ReadToEnd();
                //else Thread.Sleep(50);
            }

            return res;
        }
    }
}
