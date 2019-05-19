using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LogJoint
{
	public interface ITempFilesManager
	{
		string GenerateNewName();
		bool IsTemporaryFile(string filePath);
	};

	public interface ITempFilesCleanupList : IDisposable
	{
		void Add(string fileName);
	};

	public static class Extensions // todo: move to extensions.cs
	{
		public static void DeleteIfTemporary(this ITempFilesManager tempFiles, string fileName)
		{
			if (tempFiles.IsTemporaryFile(fileName))
				File.Delete(fileName);
		}

		public static string CreateEmptyFile(this ITempFilesManager tempFiles)
		{
			string fname = tempFiles.GenerateNewName();
			File.Create(fname).Close();
			return fname;
		}
	};
}