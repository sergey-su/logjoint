using System.Threading.Tasks;
using System.Diagnostics;
using System;
using System.IO;
using System.Xml.Linq;

namespace LogJoint.PacketAnalysis
{
	internal static class Utils
	{
		public static async Task<int> GetExitCodeAsync(
			this Process process, TimeSpan timeout, bool killOnTimeout = false)
		{
			var tcs = new TaskCompletionSource<int>();
			EventHandler handler = (s, e) => tcs.TrySetResult(process.ExitCode);
			process.EnableRaisingEvents = true;
			process.Exited += handler;
			try
			{
				if (process.HasExited)
					return process.ExitCode;
				await Task.WhenAny(Task.Delay(timeout), tcs.Task);
				if (process.HasExited)
					return process.ExitCode;
				if (killOnTimeout)
					process.Kill();
				throw new TimeoutException(string.Format("Process {0} {1} did not exit in time",
					process.Id,
					process.StartInfo != null ? Path.GetFileName(process.StartInfo.FileName) : "<unknown image>"));
			}
			finally
			{
				process.Exited -= handler;
			}
		}

		public static string AttributeValue(this XElement source, XName name, string defaultValue = "")
		{
			return source?.Attribute(name)?.Value ?? defaultValue;
		}

		static readonly DateTime unixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);

		public static DateTime UnixTimestampMillisToDateTime(double dt)
		{
			return unixEpochStart.AddMilliseconds(dt);
		}
	}
}
