using System;
using System.IO;

namespace LogJoint.LogMedia
{
	public interface IFileSystemWatcher : IDisposable
	{
		string Path { get; set; }
		event FileSystemEventHandler Created;
		event FileSystemEventHandler Changed;
		event RenamedEventHandler Renamed;
		bool EnableRaisingEvents { get; set; }
	};

	public interface IFileSystem
	{
		Stream OpenFile(string fileName);
		string[] GetFiles(string path, string searchPattern);
		IFileSystemWatcher CreateWatcher();
	};

	public interface IFileStreamInfo
	{
		DateTime LastWriteTime { get; }
		bool IsDeleted { get; }
	};
}
