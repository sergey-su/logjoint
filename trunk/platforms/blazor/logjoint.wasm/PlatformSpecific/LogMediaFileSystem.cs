using LogJoint.LogMedia;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAssemblyJSRuntime = Microsoft.JSInterop.WebAssembly.WebAssemblyJSRuntime;

namespace LogJoint.Wasm
{
    // Single-threaded
    public interface IWasmFileSystemConfig
    {
        Task<string> AddFileFromInput(ElementReference inputElement);
        Task<string> ChooseFile();
        void ReleaseFile(string fileName);
    };

    public class LogMediaFileSystem : IFileSystem, IWasmFileSystemConfig
    {
        readonly IJSRuntime jsRuntime;
        LJTraceSource traceSource = LJTraceSource.EmptyTracer;
        const string htmlInputFileNamePrefix = "/html-input/";
        const string nativeFileSystemNamePrefix = "/native-file-system/";
        int lastHtmlInputStreamId = 0;
        readonly Dictionary<string, HtmlInputFileInfo> htmlInputFiles = new Dictionary<string, HtmlInputFileInfo>();
        readonly Dictionary<string, NativeFileSystemFileInfo> nativeFileSystemFiles = new Dictionary<string, NativeFileSystemFileInfo>();

        public LogMediaFileSystem(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public void Init(ITraceSourceFactory traceFactory)
        {
            this.traceSource = traceFactory.CreateTraceSource("App", "bl-lmfs");
        }

        class HtmlInputFileInfo
        {
            public IJSRuntime jsRuntime;
            public long handle;
            public long size;
            public DateTime lastModified;

            private int refCount;

            public void AddRef()
            {
                Interlocked.Increment(ref refCount);
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref refCount) == 0)
                    Delete();
            }

            private async void Delete()
            {
                await jsRuntime.InvokeVoidAsync("logjoint.files.close", handle);
            }
        };

        class NativeFileSystemFileInfo
        {
            public IJSRuntime jsRuntime;
            public long handle;
            public long size;
            public DateTime lastModified;

            private int refCount;

            public void AddRef()
            {
                Interlocked.Increment(ref refCount);
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref refCount) == 0)
                    Delete();
            }

            private async void Delete()
            {
                await jsRuntime.InvokeVoidAsync("logjoint.nativeFiles.close", handle);
            }
        };

        class StreamImpl : FileStream, IFileStreamInfo
        {
            private readonly string fileName;

            public StreamImpl(string fileName) : base(fileName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite)
            {
                this.fileName = fileName;
            }

            DateTime IFileStreamInfo.LastWriteTime => File.GetLastWriteTime(fileName);

            bool IFileStreamInfo.IsDeleted => false;
        }

        class HtmlInputFileStream : Stream, IFileStreamInfo
        {
            readonly IJSRuntime jsRuntime;
            readonly IJSUnmarshalledRuntime webAssemblyJSRuntime;
            readonly HtmlInputFileInfo fileInfo;
            bool disposed;
            long position;

            public HtmlInputFileStream(IJSRuntime jsRuntime, HtmlInputFileInfo fileInfo)
            {
                this.jsRuntime = jsRuntime;
                this.webAssemblyJSRuntime = jsRuntime as IJSUnmarshalledRuntime;
                this.fileInfo = fileInfo;
                this.position = 0;
                fileInfo.AddRef();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (!disposed)
                {
                    disposed = true;
                    fileInfo.Release();
                }
            }

            public override long Length => fileInfo.size;
            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => true;

            public override long Position
            {
                get { return position; }
                set
                {
                    position = Math.Clamp(value, 0, fileInfo.size);
                }
            }

            DateTime IFileStreamInfo.LastWriteTime => fileInfo.lastModified;

            bool IFileStreamInfo.IsDeleted => false;

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    Position = offset;
                else if (origin == SeekOrigin.Current)
                    Position = Position + offset;
                else
                    Position = fileInfo.size - offset;
                return Position;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
            {
                if (webAssemblyJSRuntime != null)
                {
                    var tempBufferId = await jsRuntime.InvokeAsync<int>("logjoint.files.readIntoTempBuffer", fileInfo.handle, position, buffer.Length);
                    var read = webAssemblyJSRuntime.InvokeUnmarshalled<int, byte[]>("logjoint.files.readTempBuffer", tempBufferId);
                    read.CopyTo(buffer.Span);
                    position += read.Length;
                    return read.Length;
                }
                else
                {
                    var str = await jsRuntime.InvokeAsync<string>("logjoint.files.read", fileInfo.handle, position, buffer.Length);
                    var read = CopyStr(str, buffer);
                    position += read;
                    return read;
                }
            }
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
            }

            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Flush() => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        class NativeFileSystemStream : Stream, IFileStreamInfo
        {
            readonly IJSRuntime jsRuntime;
            readonly IJSUnmarshalledRuntime webAssemblyJSRuntime;
            readonly NativeFileSystemFileInfo fileInfo;
            bool disposed;
            long position;

            public NativeFileSystemStream(IJSRuntime jsRuntime, NativeFileSystemFileInfo fileInfo)
            {
                this.jsRuntime = jsRuntime;
                this.webAssemblyJSRuntime = jsRuntime as IJSUnmarshalledRuntime;
                this.fileInfo = fileInfo;
                this.position = 0;
                fileInfo.AddRef();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (!disposed)
                {
                    disposed = true;
                    fileInfo.Release();
                }
            }

            public override long Length => fileInfo.size;
            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => true;

            public override long Position
            {
                get { return position; }
                set
                {
                    position = Math.Clamp(value, 0, fileInfo.size);
                }
            }

            DateTime IFileStreamInfo.LastWriteTime => fileInfo.lastModified;

            bool IFileStreamInfo.IsDeleted => false; // todo: read it from JS

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    Position = offset;
                else if (origin == SeekOrigin.Current)
                    Position = Position + offset;
                else
                    Position = fileInfo.size - offset;
                return Position;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
            {
                if (webAssemblyJSRuntime != null)
                {
                    var tempBufferId = await jsRuntime.InvokeAsync<int>("logjoint.nativeFiles.readIntoTempBuffer", fileInfo.handle, position, buffer.Length);
                    var read = webAssemblyJSRuntime.InvokeUnmarshalled<int, byte[]>("logjoint.nativeFiles.readTempBuffer", tempBufferId);
                    read.CopyTo(buffer.Span);
                    position += read.Length;
                    return read.Length;
                }
                else
                {
                    var str = await jsRuntime.InvokeAsync<string>("logjoint.nativeFiles.read", fileInfo.handle, position, buffer.Length);
                    var read = CopyStr(str, buffer);
                    position += read;
                    return read;
                }
            }
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
            }

            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Flush() => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        IFileSystemWatcher IFileSystem.CreateWatcher() => throw new NotImplementedException();

        string[] IFileSystem.GetFiles(string path, string searchPattern) => throw new NotImplementedException();

        async Task<Stream> IFileSystem.OpenFile(string fileName)
        {
            if (htmlInputFiles.TryGetValue(fileName, out var fileInfo))
                return new HtmlInputFileStream(jsRuntime, fileInfo);
            else if (nativeFileSystemFiles.TryGetValue(fileName, out var nativeFileInfo))
                return new NativeFileSystemStream(jsRuntime, nativeFileInfo);
            else if (fileName.StartsWith(nativeFileSystemNamePrefix))
                return new NativeFileSystemStream(jsRuntime, await RestoreNativeFileHandle(long.Parse(fileName.Split('/')[2])));
            else
                return new StreamImpl(fileName);
        }

        async Task<NativeFileSystemFileInfo> RestoreNativeFileHandle(long dbId)
        {
            var handle = await jsRuntime.InvokeAsync<long>("logjoint.nativeFiles.restoreFromDatabase", dbId);
            var (fileName, fileInfo) = await EnsureNativeFileSystemFileInfoExists(handle, dbId);
            return fileInfo;
        }

        async Task<string> IWasmFileSystemConfig.AddFileFromInput(ElementReference inputElement)
        {
            var handle = await jsRuntime.InvokeAsync<long>("logjoint.files.open", inputElement);
            var size = await jsRuntime.InvokeAsync<long>("logjoint.files.getSize", handle);
            var lastModified = await jsRuntime.InvokeAsync<long>("logjoint.files.getLastModified", handle);
            var name = await jsRuntime.InvokeAsync<string>("logjoint.files.getName", handle);
            string fileName = $"{htmlInputFileNamePrefix}{++lastHtmlInputStreamId}-{name}";
            htmlInputFiles.Add(fileName, new HtmlInputFileInfo()
            {
                jsRuntime = jsRuntime,
                handle = handle,
                size = size,
                lastModified = DateTime.UnixEpoch.AddMilliseconds(lastModified)
            });
            return fileName;
        }
        async Task<string> IWasmFileSystemConfig.ChooseFile()
        {
            var handle = await jsRuntime.InvokeAsync<long>("logjoint.nativeFiles.choose");
            var dbId = await jsRuntime.InvokeAsync<long>("logjoint.nativeFiles.ensureStoredInDatabase", handle);
            var (fileName, fileInfo) = await EnsureNativeFileSystemFileInfoExists(handle, dbId);
            traceSource.Info("chosen file has been given name '{0}' and stored in database with id {1}", fileName, dbId);
            return fileName;
        }

        async Task<(string fileName, NativeFileSystemFileInfo fileInfo)> EnsureNativeFileSystemFileInfoExists(long handle, long dbId)
        {
            var name = await jsRuntime.InvokeAsync<string>("logjoint.nativeFiles.getName", handle);
            string fileName = $"{nativeFileSystemNamePrefix}{dbId}/{name}";
            if (!nativeFileSystemFiles.TryGetValue(fileName, out var fileInfo))
            {
                var size = await jsRuntime.InvokeAsync<long>("logjoint.nativeFiles.getSize", handle);
                var lastModified = await jsRuntime.InvokeAsync<long>("logjoint.nativeFiles.getLastModified", handle);
                nativeFileSystemFiles.Add(fileName, fileInfo = new NativeFileSystemFileInfo()
                {
                    jsRuntime = jsRuntime,
                    handle = handle,
                    size = size,
                    lastModified = DateTime.UnixEpoch.AddMilliseconds(lastModified)
                });
            }
            return (fileName, fileInfo);
        }

        void IWasmFileSystemConfig.ReleaseFile(string fileName)
        {
            if (htmlInputFiles.TryGetValue(fileName, out var stream))
            {
                htmlInputFiles.Remove(fileName);
                stream.Release();
            }
        }

        static int CopyStr(string s, Memory<byte> buffer)
        {
            int read = s.Length;
            var dest = buffer.Span;
            for (int i = 0; i < read; ++i)
                dest[i] = unchecked((byte)s[i]);
            return read;
        }
    }
}
