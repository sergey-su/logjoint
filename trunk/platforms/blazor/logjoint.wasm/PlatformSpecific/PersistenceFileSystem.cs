using System;
using System.IO;
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

        public PersistenceFileSystem(IJSInProcessRuntime jsRuntime, IndexedDB indexedDB)
        {
            this.jsRuntime = jsRuntime;
            this.indexedDB = indexedDB;

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
            var key = Key(relativePath);
            if (await Get(key) == null)
                await Set(key, "*");
        }
        async Task<Stream> IFileSystemAccess.OpenFile(string relativePath, bool readOnly)
        {
            var key = Key(relativePath);
            var value = await Get(key);
            if (value == null && readOnly)
                return null;
            Func<string, ValueTask> newValueSetter = null;
            if (!readOnly)
                newValueSetter = v => Set(key, v);
            return new StreamImpl(value, newValueSetter);
        }

        public string AbsoluteRootPath => "";
        async Task<long> IFileSystemAccess.CalcStorageSize(CancellationToken cancellation) => 0;
        void IFileSystemAccess.ConvertException(Exception e)
        {
            if (e.Message.Contains("exceeded the quota"))
                throw new StorageFullException(e);
            throw new StorageException(e);
        }
        async Task<string[]> IFileSystemAccess.ListDirectories(string rootRelativePath, CancellationToken cancellation)
        {
            return new string[] {};
        }
        async Task<string[]> IFileSystemAccess.ListFiles(string rootRelativePath, CancellationToken cancellation)
        {
            return new string[] {};
        }
        async Task IFileSystemAccess.DeleteDirectory(string relativePath) { }

        static string Key(string relativePath)
        {
            return relativePath;
        }
        async ValueTask<string> Get(string key)
        {
            return await indexedDB.Get<string>("persistence", key);
        }
        async ValueTask Set(string key, string value)
        {
            await indexedDB.Set("persistence", value, key);
        }

        bool Persistence.IFirstStartDetector.IsFirstStartDetected => isFirstStart;

        class StreamImpl : MemoryStream, IDisposable
        {
            readonly Func<string, ValueTask> newValueSetter;
            bool dirty;

            public StreamImpl(string initialValue, Func<string, ValueTask> newValueSetter)
            {
                this.newValueSetter = newValueSetter;
                if (initialValue != null)
                {
                    Write(Convert.FromBase64String(initialValue));
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
                await newValueSetter(Convert.ToBase64String(this.ToArray()));
                dirty = false;
            }
        }   
    }
}

