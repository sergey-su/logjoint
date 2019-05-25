using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol;
using Newtonsoft.Json;

namespace LogJoint.Postprocessing.Correlation.ExternalSolver
{
	public class CmdLineToolProxy : ExternalSolverBase
	{
		protected override Response Solve(Request request, CancellationToken cancellation)
		{
			var jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented
			});
			var binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var pi = new ProcessStartInfo()
			{
				FileName = "mono64",
				Arguments = Path.Combine(binDir, "logjoint.ortoolswrp.exe"),
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			pi.EnvironmentVariables["DYLD_FALLBACK_LIBRARY_PATH"] = Path.Combine(binDir, "ortools") + ":";
			var proc = Process.Start(pi);
			jsonSerializer.Serialize(proc.StandardInput, request);
			proc.StandardInput.Close();
			proc.WaitForExit();
			if (proc.ExitCode != 0)
				throw new Exception(string.Format("external solver failed with code {0}: {1}",
					proc.ExitCode, proc.StandardOutput.ReadToEnd()));
			return (Protocol.Response)jsonSerializer.Deserialize(
				proc.StandardOutput, typeof(Protocol.Response));
		}
	}
}