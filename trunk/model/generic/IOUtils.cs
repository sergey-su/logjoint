using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace LogJoint
{
	public static class IOUtils
	{
		/// <summary>
		/// Does basic path normalization:
		///    ensures path is absolute,
		///    makes path lowercase
		/// </summary>
		public static string NormalizePath(string path)
		{
			return Path.GetFullPath(path).ToLower();
		}

		public static void CopyStreamWithProgress(Stream src, Stream dest, Action<long> progress, CancellationToken cancellation)
		{
			for (byte[] buf = new byte[16 * 1024]; ;)
			{
				int read = src.Read(buf, 0, buf.Length);
				if (read == 0)
					break;
				cancellation.ThrowIfCancellationRequested();
				dest.Write(buf, 0, read);
				progress(dest.Length);
			}
		}

		public static async Task CopyStreamWithProgressAsync(Stream src, Stream dest, Action<long> progress, CancellationToken cancellation)
		{
			for (byte[] buf = new byte[16 * 1024]; ; )
			{
				int read = await src.ReadAsync(buf, 0, buf.Length);
				if (read == 0)
					break;
				cancellation.ThrowIfCancellationRequested();
				await dest.WriteAsync(buf, 0, read);
				progress(dest.Length);
			}
		}

		public static string FileSizeToString(long fileSize)
		{
			const int byteConversion = 1024;
			double bytes = Convert.ToDouble(fileSize);

			if (bytes >= Math.Pow(byteConversion, 3)) //GB Range
			{
				return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 3), 2), " GB");
			}
			else if (bytes >= Math.Pow(byteConversion, 2)) //MB Range
			{
				return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 2), 2), " MB");
			}
			else if (bytes >= byteConversion) //KB Range
			{
				return string.Concat(Math.Round(bytes / byteConversion, 2), " KB");
			}
			else //Bytes
			{
				return string.Concat(bytes, " Bytes");
			}
		}

		public static void EnsureIsExecutable(string executablePath)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				File.SetAttributes(
					executablePath,
					(FileAttributes)((uint) File.GetAttributes (executablePath) | 0x80000000)
				);
		}

		public static async Task CheckPythonInstallation()
		{
			var pi = new ProcessStartInfo()
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				FileName = "python",
				Arguments = "--help"
			};
			bool testFailed = false;
			try
			{
				using (var process = Process.Start(pi))
					testFailed = await process.GetExitCodeAsync(TimeSpan.FromSeconds(10)) != 0;
			}
			catch
			{
				testFailed = true;
			}
			if (testFailed)
				throw new Exception("python not found. install it and put to PATH");
		}

		public static bool IsBinaryFile(Stream stm)
		{
			int nrOfProbs = 8;
			int probeSz = 1024;

			Predicate<byte> isBinary = c => (c < 8) || (c > 13 && c < 26);

			var buf = new byte[probeSz];
			long stmSz = stm.Length;
			stm.Position = 0;
			for (int probeIdx = 0; probeIdx < nrOfProbs; ++probeIdx)
			{
				stm.Position = probeIdx * Math.Max(0, stmSz - probeSz) / nrOfProbs;
				int read = stm.Read(buf, 0, probeSz);
				for (int i = 0; i < read; ++i)
					if (isBinary(buf[i]))
						return true;
			}
			return false;
		}
	}
}
