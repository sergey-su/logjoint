using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

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
	
		public static void CopyStreamWithProgress(Stream src, Stream dest, Action<long> progress)
		{
			for (byte[] buf = new byte[16 * 1024]; ; )
			{
				int read = src.Read(buf, 0, buf.Length);
				if (read == 0)
					break;
				dest.Write(buf, 0, read);
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

		#if MONOMAC
		public static void EnsureIsExecutable(string executablePath)
		{
			File.SetAttributes(
				executablePath,
				(FileAttributes)((uint) File.GetAttributes (executablePath) | 0x80000000)
			);
		}
		#else
		public static void EnsureIsExecutable(string executablePath)
		{
		}
		#endif
	}
}
