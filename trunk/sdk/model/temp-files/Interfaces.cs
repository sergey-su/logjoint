using System;

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
}