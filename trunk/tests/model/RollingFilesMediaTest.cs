﻿using System;
using System.Text;
using System.Collections.Generic;
using LogJoint.LogMedia;
using LogJoint;
using System.IO;
using System.Text.RegularExpressions;
using LogJoint.Settings;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LogJoint.Tests
{
    [TestFixture]
    public class RollingFilesMediaTest
    {
        class FileSystemWatcherImpl : IFileSystemWatcher
        {
            readonly FileSystemImpl fileSystem;
            string path;
            bool enabled;

            public FileSystemWatcherImpl(FileSystemImpl fs)
            {
                fileSystem = fs;
                fs.watchers.Add(this);
            }

            public void FireChanged(string fileName)
            {
                if (enabled)
                    Changed?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, fileSystem.BaseDir, fileName));
            }

            public void FireCreated(string fileName)
            {
                if (enabled)
                    Created?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, fileSystem.BaseDir, fileName));
            }

            #region IFileSystemWatcher Members

            public string Path
            {
                get
                {
                    return path;
                }
                set
                {
                    Assert.That(fileSystem.BaseDir.ToLower(), Is.EqualTo(value.ToLower()));
                    path = value;
                }
            }

            public event FileSystemEventHandler Created;

            public event FileSystemEventHandler Changed;

            public event RenamedEventHandler Renamed;

            public bool EnableRaisingEvents
            {
                get
                {
                    return enabled;
                }
                set
                {
                    enabled = value;
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                fileSystem.watchers.Remove(this);
            }

            #endregion

            protected void FireRenamed()
            {
                Renamed?.Invoke(this, null);
            }
        };

        class FileSystemImpl : IFileSystem
        {
            class FileImpl : MemoryStream, IFileStreamInfo
            {
                public DateTime writeTime = new DateTime();
                public int openCounter;
                public bool isDeleted;

                public override void Close()
                {
                    Assert.That(openCounter, Is.GreaterThan(0));
                    openCounter--;
                }

                public DateTime LastWriteTime
                {
                    get
                    {
                        Assert.That(openCounter, Is.GreaterThan(0));
                        return this.writeTime;
                    }
                }

                public bool IsDeleted
                {
                    get
                    {
                        Assert.That(openCounter, Is.GreaterThan(0));
                        return isDeleted;
                    }
                }

            };
            readonly Dictionary<string, FileImpl> files = new Dictionary<string, FileImpl>();
            readonly string baseDir;
            internal readonly List<FileSystemWatcherImpl> watchers = new List<FileSystemWatcherImpl>();

            public string BaseDir { get { return baseDir; } }
            public void AddFile(string name, int data)
            {
                name = name.ToLower();
                if (!files.ContainsKey(name))
                    files.Add(name, new FileImpl());
                SetData(name, data);
                foreach (FileSystemWatcherImpl w in watchers)
                {
                    w.FireCreated(name);
                }
            }
            public void RemoveFile(string name)
            {
                name = name.ToLower();
                files[name].isDeleted = true;
                Assert.That(files.Remove(name), Is.True);
            }

            public void SetData(string name, int data)
            {
                name = name.ToLower();
                FileImpl f = files[name];
                f.SetLength(0);
                StreamWriter w = new StreamWriter(f);
                w.Write(data.ToString());
                w.Flush();
                foreach (FileSystemWatcherImpl wtch in watchers)
                {
                    wtch.FireChanged(Path.Combine(baseDir, name));
                }
            }

            #region IFileSystem

            public Task<Stream> OpenFile(string fileName)
            {
                Assert.That(baseDir.ToLower(), Is.EqualTo(Path.GetDirectoryName(fileName).ToLower()));
                if (!files.TryGetValue(Path.GetFileName(fileName).ToLower(), out FileImpl ret))
                    throw new FileNotFoundException();
                ret.openCounter++;
                return Task.FromResult<Stream>(ret);
            }

            public static string WildcardToRegex(string pattern)
            {
                return "^" + Regex.Escape(pattern).
                    Replace("\\*", ".*").
                    Replace("\\?", ".") + "$";
            }

            public string[] GetFiles(string path, string searchPattern)
            {
                Assert.That(baseDir.ToLower(), Is.EqualTo(path.ToLower()));

                List<string> ret = new List<string>();
                Regex re = new Regex(WildcardToRegex(searchPattern));
                foreach (string fname in files.Keys)
                {
                    if (re.Match(fname).Success)
                        ret.Add(Path.Combine(baseDir, fname));
                }

                return ret.ToArray();
            }

            public DateTime GetLastWriteTime(string fileName)
            {
                Assert.That(baseDir.ToLower(), Is.EqualTo(Path.GetDirectoryName(fileName).ToLower()));
                if (!files.TryGetValue(Path.GetFileName(fileName).ToLower(), out FileImpl f))
                    throw new IOException();
                return f.LastWriteTime;
            }

            public IFileSystemWatcher CreateWatcher()
            {
                return new FileSystemWatcherImpl(this);
            }

            #endregion

            public FileSystemImpl(string baseDir)
            {
                this.baseDir = baseDir;
            }
            public FileSystemImpl()
#if MONO
				: this("/Users/fake folder that does not exist")
#else
                : this("c:\\fake folder that doesn't exist")
#endif
            {
            }
        };

        class MessagesReader : IMessagesReader
        {
            ILogMedia media;
            public const int EmptyFileContent = 0;
            public const int InvalidFileContent = 666;

            public MessagesReader(MediaBasedReaderParams readerParams)
            {
                this.Media = readerParams.Media;
            }

            #region IMessagesReader Members

            public long BeginPosition
            {
                get { return 0; }
            }

            public long EndPosition
            {
                get { return Media.Size; }
            }

            public async Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode)
            {
                await Media.Update();
                return UpdateBoundsStatus.NewMessagesAvailable;
            }

            public long CalcMaxActiveRangeSize(IGlobalSettingsAccessor settings)
            {
                throw new NotImplementedException();
            }

            public long MaximumMessageSize
            {
                get { throw new NotImplementedException(); }
            }

            public long PositionRangeToBytes(LogJoint.FileRange.Range range)
            {
                throw new NotImplementedException();
            }

            public long SizeInBytes
            {
                get { throw new NotImplementedException(); }
            }

            public ITimeOffsets TimeOffsets
            {
                get { return LogJoint.TimeOffsets.Empty; }
                set { }
            }

            public ILogMedia Media { get => media; set => media = value; }

            ValueTask<int> IMessagesReader.GetContentsEtag()
            {
                return ValueTask.FromResult(0);
            }

            Encoding IMessagesReader.Encoding => Encoding.ASCII;

            public async IAsyncEnumerable<PostprocessedMessage> Read(ReadMessagesParams p)
            {
                DateTime time = new DateTime();
                DateTime startOfTime = new DateTime(2000, 1, 1);
                bool messageRead = false;
                Media.DataStream.Position = 0;
                TextReader r = new StreamReader(Media.DataStream);
                int val = int.Parse(r.ReadToEnd().Trim());
                if (val == EmptyFileContent)
                {
                    messageRead = true;
                }
                else if (val == InvalidFileContent)
                {
                    throw new InvalidFormatException();
                }
                else
                {
                    time = startOfTime.Add(TimeSpan.FromHours(val));
                }


                for (; ; )
                {
                    if (messageRead)
                        yield break;
                    messageRead = true;
                    IMessage m = new Message(0, 1, null, new MessageTimestamp(time), StringSlice.Empty, SeverityFlag.Info);
                    yield return new PostprocessedMessage(m, null);
                }
            }

            public IAsyncEnumerable<SearchResultMessage> Search(SearchMessagesParams p)
            {
                return null;
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion
        }

        static ILogMedia CreateMedia(FileSystemImpl fs)
        {
            var media = new RollingFilesMedia(
                fs,
                @params => new MessagesReader(@params),
                LJTraceSource.EmptyTracer,
                new GenericRollingMediaStrategy(fs.BaseDir, Array.Empty<string>())
            );
            return media;
        }

        static void CheckMedia(ILogMedia media, string expectedContent)
        {
            media.DataStream.Position = 0;
            StreamReader r = new StreamReader(media.DataStream);
            string actual = r.ReadToEnd();
            Assert.That(actual, Is.EqualTo(expectedContent));
            Assert.That(media.Size, Is.EqualTo((long)expectedContent.Length));
        }

        [Test]
        public async Task OpeningFilesThatAreEnumeratedInValidOrder()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "123");
            }
        }

        [Test]
        public async Task OpeningFilesThatAreEnumeratedInInvalidOrder()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 2);
            fs.AddFile("a1.log", 1);
            fs.AddFile("a2.log", 4);
            fs.AddFile("a3.log", 3);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "1234");
            }
        }

        [Test]
        public async Task InvaidFileInTheGroupMustBeIgnored()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", MessagesReader.InvalidFileContent);
            fs.AddFile("a3.log", 4);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "124");
            }
        }

        [Test]
        public async Task EmptyFileInTheGroupMustBeIgnored()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", MessagesReader.EmptyFileContent);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", MessagesReader.EmptyFileContent);
            fs.AddFile("a3.log", 4);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "24");
            }
        }

        [Test]
        public async Task LastFileGrowing()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            fs.AddFile("a3.log", 4);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "1234");

                fs.SetData("a3.log", 89);
                await media.Update();
                CheckMedia(media, "12389");
            }
        }

        [Test]
        public async Task LastFileGetsSmaller()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            fs.AddFile("a3.log", 89);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "12389");

                fs.SetData("a3.log", 4);
                await media.Update();
                CheckMedia(media, "1234");
            }
        }

        [Test]
        public async Task NewLastFileCreated()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "123");

                fs.AddFile("a3.log", 4);
                await media.Update();
                CheckMedia(media, "1234");
            }
        }

        [Test]
        public async Task NewIntermediateFileCreated()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a3.log", 4);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "124");

                fs.AddFile("a2.log", 3);
                await media.Update();
                CheckMedia(media, "1234");
            }
        }

        [Test]
        public async Task IntermediateFileDeleted()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "123");

                fs.RemoveFile("a1.log");
                await media.Update();
                CheckMedia(media, "13");
            }
        }

        [Test]
        public async Task RepeatedCreationNotificationsAreHandledCorrectly()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("a0.log", 1);
            fs.AddFile("a1.log", 2);
            fs.AddFile("a2.log", 3);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "123");

                // Create new file over existing one
                fs.AddFile("a1.log", 4);
                await media.Update();

                CheckMedia(media, "134");
            }
        }

        [Test]
        public async Task FileNamesAreNotCaseSensitive()
        {
            FileSystemImpl fs = new FileSystemImpl();
            fs.AddFile("abc1.log", 1);
            fs.AddFile("abc2.log", 2);
            using (var media = CreateMedia(fs))
            {
                await media.Update();
                CheckMedia(media, "12");

                // Create new file over existing one
                fs.AddFile("aBC1.log", 4);
                await media.Update();

                CheckMedia(media, "24");
            }
        }


    }


}
