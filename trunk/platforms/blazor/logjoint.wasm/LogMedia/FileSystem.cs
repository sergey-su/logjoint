using LogJoint.LogMedia;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    // Single-threaded
    public interface IWasmFileSystemConfig
    {
        Task<string> AddFileFromInput(ElementReference inputElement);
        void ReleaseFile(string fileName);
    };

    public class FileSystem : IFileSystem, IWasmFileSystemConfig
    {
        readonly IJSRuntime jsRuntime;
        const string htmlInputFileNamePrefix = "/html-input/";
        int lastHtmlInputStreamId = 0;
        readonly Dictionary<string, HtmlInputFileInfo> htmlInputFiles = new Dictionary<string, HtmlInputFileInfo>();

        public FileSystem(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
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
            readonly HtmlInputFileInfo fileInfo;
            bool disposed;
            long position;

            public HtmlInputFileStream(IJSRuntime jsRuntime, HtmlInputFileInfo fileInfo)
            {
                this.jsRuntime = jsRuntime;
                this.fileInfo = fileInfo;
                this.position = 0;
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
                var str = await jsRuntime.InvokeAsync<string>("logjoint.files.read", fileInfo.handle, position, buffer.Length);
                var read = str.Length;
                for (int i = 0; i < read; ++i)
                {
                    buffer.Span[i] = (byte)str[i];
                }
                position += read;
                return read;
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

        Stream IFileSystem.OpenFile(string fileName)
        {
            if (htmlInputFiles.TryGetValue(fileName, out var fileInfo))
                return new HtmlInputFileStream(jsRuntime, fileInfo);
            else
                return new StreamImpl(fileName);
        }

        async Task<string> IWasmFileSystemConfig.AddFileFromInput(ElementReference inputElement)
        {
            var handle = await jsRuntime.InvokeAsync<long>("logjoint.files.open", inputElement);
            var size = await jsRuntime.InvokeAsync<long>("logjoint.files.getSize", handle);
            var lastModified = await jsRuntime.InvokeAsync<long>("logjoint.files.getLastModified", handle);
            string fileName = $"{htmlInputFileNamePrefix}{++lastHtmlInputStreamId}";
            htmlInputFiles.Add(fileName, new HtmlInputFileInfo()
            {
                jsRuntime = jsRuntime,
                handle = handle,
                size = size,
                lastModified = DateTime.UnixEpoch.AddMilliseconds(lastModified)
            });
            return fileName;
        }

        void IWasmFileSystemConfig.ReleaseFile(string fileName)
        {
            if (htmlInputFiles.TryGetValue(fileName, out var stream))
            {
                htmlInputFiles.Remove(fileName);
                stream.Release();
            }
        }
    }
}
