using System;
using System.Diagnostics;
using System.IO;

namespace LogJoint.Wireshark.Dpml
{
	public class TShark: ITShark
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

#if MONOMAC
		private string GetExecutablePath()
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
			return null;
		}
#elif WIN
		private string GetExecutablePath()
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
#endif
	};
}
