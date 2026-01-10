using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Persistence.Implementation
{
    public class DesktopFileSystemAccess : IFileSystemAccess, IFirstStartDetector
    {
        public DesktopFileSystemAccess(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
            if (!Directory.Exists(this.rootDirectory))
            {
                Directory.CreateDirectory(rootDirectory);
                firstStartDetected = true;
            }
        }

        static public DesktopFileSystemAccess CreatePersistentUserDataFileSystem(string? appDataDirectory = null)
        {
            return new DesktopFileSystemAccess(string.Format("{0}{1}LogJoint{1}",
                appDataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.DirectorySeparatorChar));
        }

        static public DesktopFileSystemAccess CreateCacheFileSystemAccess(string? appDataDirectory = null)
        {
            return new DesktopFileSystemAccess(string.Format("{0}{1}LogJoint{1}Cache{1}",
                appDataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.DirectorySeparatorChar));
        }

        void IFileSystemAccess.SetTrace(LJTraceSource trace)
        {
            this.trace = trace;
        }

        void IFileSystemAccess.ConvertException(Exception e)
        {
            var io = e as IOException;
            if (io != null)
            {
                if ((long)io.HResult == 0x80070070) // todo: do it differently on mac/mono
                    throw new StorageFullException(e);
            }
            throw new StorageException(e);
        }

        bool IFirstStartDetector.IsFirstStartDetected { get { return firstStartDetected; } }

        public Task EnsureDirectoryCreated(string dirName)
        {
            // CreateDirectory doesn't fail is dir already exists
            Directory.CreateDirectory(rootDirectory + dirName);
            return Task.CompletedTask;
        }

        public async Task<Stream> OpenFile(string relativePath, bool readOnly)
        {
            // It is a common case when existing file is opened for reading.
            // Handle that without throwing hidden exceptions.
            if (readOnly && !File.Exists(rootDirectory + relativePath))
                return null;

            int maxTryCount = 10;
            int millisecsToWaitBetweenTries = 50;

            for (int tryIdx = 0; ; ++tryIdx)
            {
                try
                {
                    var ret = new FileStream(rootDirectory + relativePath,
                        readOnly ? FileMode.Open : FileMode.OpenOrCreate,
                        readOnly ? FileAccess.Read : FileAccess.ReadWrite,
                        FileShare.None);
                    return ret;
                }
                catch (Exception e)
                {
                    trace.Warning("Failed to open file {0}: {1}", relativePath, e.Message);
                    if (tryIdx >= maxTryCount)
                    {
                        trace.Error(e, "No more tries. Giving up");
                        if (readOnly)
                            return null;
                        else
                            throw;
                    }
                    trace.Info("Will try agian. Tries left: {0}", maxTryCount - tryIdx);
                    await Task.Delay(millisecsToWaitBetweenTries);
                }
            }
        }

        public Task<string[]> ListDirectories(string rootRelativePath, CancellationToken cancellation)
        {
            return Task.FromResult(Directory.EnumerateDirectories(rootDirectory + rootRelativePath).Select(dir =>
            {
                cancellation.ThrowIfCancellationRequested();
                if (rootRelativePath == "")
                    return Path.GetFileName(dir);
                else
                    return rootRelativePath + Path.DirectorySeparatorChar + Path.GetFileName(dir);
            }).ToArray());
        }

        public Task<string[]> ListFiles(string rootRelativePath, CancellationToken cancellation)
        {
            return Task.FromResult(Directory.EnumerateFiles(rootDirectory + rootRelativePath).Select(fileName =>
            {
                cancellation.ThrowIfCancellationRequested();
                if (rootRelativePath == "")
                    return Path.GetFileName(fileName);
                else
                    return rootRelativePath + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
            }).ToArray());
        }

        public Task DeleteDirectory(string relativePath)
        {
            Directory.Delete(rootDirectory + relativePath, true);
            return Task.CompletedTask;
        }

        static long CalcDirSize(DirectoryInfo d, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            long ret = 0;
            ret = d.EnumerateFiles().Aggregate(ret, (c, fi) => { cancellation.ThrowIfCancellationRequested(); return c + fi.Length; });
            ret = d.EnumerateDirectories().Aggregate(ret, (c, di) => c + CalcDirSize(di, cancellation));
            return ret;
        }

        public Task<long> CalcStorageSize(CancellationToken cancellation)
        {
            return Task.FromResult(CalcDirSize(new DirectoryInfo(rootDirectory), cancellation));
        }

        public string AbsoluteRootPath { get { return rootDirectory; } }

        LJTraceSource trace = LJTraceSource.EmptyTracer;
        readonly string rootDirectory;
        readonly bool firstStartDetected;
    };
}
