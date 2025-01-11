using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.LogMedia
{
    // Single-threaded.
    public interface IFileSystemWatcher : IDisposable
    {
        string Path { get; set; }
        event FileSystemEventHandler Created;
        event FileSystemEventHandler Changed;
        event RenamedEventHandler Renamed;
        bool EnableRaisingEvents { get; set; }
    };

    // Thread-safe.
    public interface IFileSystem
    {
        Task<Stream> OpenFile(string fileName);
        string[] GetFiles(string path, string searchPattern);
        IFileSystemWatcher CreateWatcher();
    };

    public interface IFileStreamInfo
    {
        DateTime LastWriteTime { get; }
        bool IsDeleted { get; }
    };
}
