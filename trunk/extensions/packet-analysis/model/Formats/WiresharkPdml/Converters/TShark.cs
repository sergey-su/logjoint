using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LogJoint.Wireshark.Dpml
{
    public class TShark : ITShark
    {
        string executablePath;

        public bool IsAvailable
        {
            get
            {
                if (executablePath == null)
                    executablePath = GetExecutablePath();
                return !string.IsNullOrEmpty(executablePath);
            }
        }

        public Process Start(string args)
        {
            if (!IsAvailable)
                throw new Exception("tshark is not installed on your system");
            var psi = new ProcessStartInfo()
            {
                FileName = executablePath,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            return Process.Start(psi);
        }

        private string GetExecutablePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetExecutablePathOSX();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetExecutablePathWin();
            else
                throw new PlatformNotSupportedException();
        }

        private string GetExecutablePathOSX()
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "which",
                Arguments = "tshark",
                UseShellExecute = false
            };
            using (var tester = Process.Start(psi))
            {
                tester.WaitForExit();
                if (tester.ExitCode == 0)
                    return "tshark";
            }
            if (File.Exists("/usr/local/bin/tshark"))
            {
                return "/usr/local/bin/tshark";
            }
            return null;
        }

        private string GetExecutablePathWin()
        {
            foreach (var folder in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                var fname = Path.Combine(folder, "Wireshark", "tshark.exe");
                if (File.Exists(fname))
                    return fname;
            }
            return null;
        }
    };
}
