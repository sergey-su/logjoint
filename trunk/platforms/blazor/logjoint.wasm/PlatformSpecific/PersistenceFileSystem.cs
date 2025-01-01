using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.Persistence;
using LogJoint.Persistence.Implementation;
using Microsoft.JSInterop;

namespace LogJoint.Wasm
{
    public class PersistenceFileSystem : IFileSystemAccess, Persistence.IFirstStartDetector
    {
        private readonly IJSInProcessRuntime jsRuntime;
        readonly bool isFirstStart;
        private LJTraceSource trace;
        private readonly IndexedDB indexedDB;
        private readonly string dbStoreName;
        const string directoryPrefix = "/";

        public PersistenceFileSystem(IJSInProcessRuntime jsRuntime, IndexedDB indexedDB, string dbStoreName)
        {
            this.jsRuntime = jsRuntime;
            this.indexedDB = indexedDB;
            this.dbStoreName = dbStoreName;

            this.isFirstStart = jsRuntime.Invoke<string>("logjoint.getLocalStorageItem", "started") == null;
            if (this.isFirstStart)
                jsRuntime.InvokeVoid("logjoint.setLocalStorageItem", "started", "*");
        }

        void IFileSystemAccess.SetTrace(LJTraceSource trace)
        {
            this.trace = trace;
        }
        async Task IFileSystemAccess.EnsureDirectoryCreated(string relativePath)
        {
            var key = DirectoryKey(relativePath);
            if (await Get(key) == null)
                await Set(key, new byte[] { 42 });
        }
        async Task<Stream> IFileSystemAccess.OpenFile(string relativePath, bool readOnly)
        {
            var key = FileKey(relativePath);
            var value = await Get(key);
            if (value == null && readOnly)
                return null;
            Func<byte[], ValueTask> newValueSetter = null;
            if (!readOnly)
                newValueSetter = v => Set(key, v);
            return new StreamImpl(value, newValueSetter);
        }

        public string AbsoluteRootPath => throw new NotImplementedException("Can not get absolute path in web");

        async Task<long> IFileSystemAccess.CalcStorageSize(CancellationToken cancellation) => await indexedDB.EstimateSize(dbStoreName);

        void IFileSystemAccess.ConvertException(Exception e)
        {
            if (e.Message.Contains("exceeded the quota"))
                throw new StorageFullException(e);
            throw new StorageException(e);
        }
        async Task<string[]> IFileSystemAccess.ListDirectories(string rootRelativePath, CancellationToken cancellation)
        {
            return (await indexedDB.Keys(dbStoreName, directoryPrefix))
                .Select(key => key[directoryPrefix.Length..])
                .ToArray();
        }
        async Task<string[]> IFileSystemAccess.ListFiles(string rootRelativePath, CancellationToken cancellation)
        {
            return await indexedDB.Keys(dbStoreName, rootRelativePath);
        }
        async Task IFileSystemAccess.DeleteDirectory(string relativePath)
        {
            await indexedDB.DeleteByPrefix(dbStoreName, relativePath);
            await indexedDB.Delete(dbStoreName, DirectoryKey(relativePath));
        }

        static string FileKey(string relativePath)
        {
            return relativePath;
        }
        static string DirectoryKey(string relativePath)
        {
            return $"{directoryPrefix}{relativePath}";
        }
        async ValueTask<byte[]> Get(string key)
        {
            return await indexedDB.Get<byte[]>(dbStoreName, key);
        }
        async ValueTask Set(string key, byte[] value)
        {
            await indexedDB.Set(dbStoreName, value, key);
        }

        bool Persistence.IFirstStartDetector.IsFirstStartDetected => isFirstStart;

        class StreamImpl : MemoryStream, IDisposable
        {
            readonly Func<byte[], ValueTask> newValueSetter;
            bool dirty;

            public StreamImpl(byte[] initialValue, Func<byte[], ValueTask> newValueSetter)
            {
                this.newValueSetter = newValueSetter;
                if (initialValue != null)
                {
                    Write(initialValue);
                    Position = 0;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (dirty)
                    {
                        // todo: support async dispose
                        throw new InvalidOperationException("Stream has been modified since last flush");
                    }
                }
                base.Dispose(disposing);
            }

            public override void SetLength(long value)
            {
                dirty = true;
                base.SetLength(value);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                dirty = true;
                return base.WriteAsync(buffer, cancellationToken);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                dirty = true;
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await base.FlushAsync(cancellationToken);
                await newValueSetter(this.ToArray());
                dirty = false;
            }
        }
    }
}

