using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LogJoint.LogMedia;
using System.Threading.Tasks;

namespace LogJoint
{
    public class SimpleFileMedia : ILogMedia
    {
        static readonly string fileNameParam = ConnectionParamsKeys.PathConnectionParam;
        static readonly MemoryStream emptyMemoryStream = new MemoryStream();
        readonly IFileSystem fileSystem;
        readonly DelegatingStream stream = new DelegatingStream();
        bool disposed;
        string fileName;
        IFileStreamInfo fsInfo;
        DateTime lastModified;
        long size;

        public static IConnectionParams CreateConnectionParamsFromFileName(string fileName)
        {
            return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
        }

        public static async Task<SimpleFileMedia> Create(IFileSystem fileSystem, IConnectionParams connectParams)
        {
            var media = new SimpleFileMedia(fileSystem, connectParams);
            try
            {
                await media.Init();
                await media.Update();
                return media;
            }
            catch
            {
                media.Dispose();
                throw;
            }
        }


        private SimpleFileMedia(IFileSystem fileSystem, IConnectionParams connectParams)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            this.fileName = connectParams[fileNameParam];
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Invalid or incomplete connection params");
        }

        async Task Init()
        {
            Stream fs = await fileSystem.OpenFile(fileName);
            this.stream.SetStream(fs, true);
            this.fsInfo = (IFileStreamInfo)fs;
        }

        public string FileName
        {
            get { return fileName; }
        }

        #region ILogMedia Members

        public bool IsAvailable
        {
            get { return fsInfo != null; }
        }

        public async Task Update()
        {
            CheckDisposed();

            if (fsInfo == null)
            {
                Stream fs;
                try
                {
                    fs = await fileSystem.OpenFile(fileName);
                }
                catch
                {
                    return;
                }
                this.stream.SetStream(fs, true);
                this.fsInfo = (IFileStreamInfo)fs;
            }

            if (fsInfo.IsDeleted)
            {
                size = 0;
                lastModified = new DateTime();
                stream.SetStream(emptyMemoryStream, false);
                fsInfo = null;
                return;
            }

            size = stream.Length;
            lastModified = fsInfo.LastWriteTime;
        }

        public Stream DataStream
        {
            get
            {
                CheckDisposed();
                return stream;
            }
        }

        public DateTime LastModified
        {
            get
            {
                CheckDisposed();
                return lastModified;
            }
        }

        public long Size
        {
            get
            {
                CheckDisposed();
                return size;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            disposed = true;
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        #endregion

        void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name + " " + fileName);
        }

    }
}
