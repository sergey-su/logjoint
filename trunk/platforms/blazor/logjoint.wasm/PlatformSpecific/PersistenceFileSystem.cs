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
        private const string keyPrefix = "/ljpfs";
        readonly bool isFirstStart;
        private LJTraceSource trace;

        public PersistenceFileSystem(IJSInProcessRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;

            this.isFirstStart = Get("started") == null;
            if (this.isFirstStart)
                Set("started", "*");
        }

        void IFileSystemAccess.SetTrace(LJTraceSource trace)
        {
            this.trace = trace;
        }
        async Task IFileSystemAccess.EnsureDirectoryCreated(string relativePath)
        {
            var key = Key(relativePath);
            if (Get(key) == null)
                Set(key, "*");
        }
        async Task<Stream> IFileSystemAccess.OpenFile(string relativePath, bool readOnly)
        {
            var key = Key(relativePath);
            var value = Get(key);
            if (value == null && readOnly)
                return null;
            Action<string> newValueSetter = null;
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
            return Path.Combine(keyPrefix, relativePath);
        }
        string Get(string key)
        {
            return jsRuntime.Invoke<string>("logjoint.getLocalStorageItem", key);
        }
        void Set(string key, string value)
        {
            jsRuntime.InvokeVoid("logjoint.setLocalStorageItem", key, value);
        }

        bool Persistence.IFirstStartDetector.IsFirstStartDetected => isFirstStart;

        class StreamImpl : MemoryStream, IDisposable
        {
            readonly Action<string> newValueSetter;

            public StreamImpl(string initialValue, Action<string> newValueSetter)
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
                if (disposing && newValueSetter != null)
                {
                    newValueSetter(Convert.ToBase64String(this.ToArray()));
                }
                base.Dispose(disposing);
            }
        };
    }
}

