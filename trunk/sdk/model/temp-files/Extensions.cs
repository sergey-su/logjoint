using System.IO;

namespace LogJoint
{
	public static class Extensions
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