using System;
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
					if (Changed != null)
						Changed(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, fileSystem.BaseDir, fileName));
			}

			public void FireCreated(string fileName)
			{
				if (enabled)
					if (Created != null)
						Created(this, new FileSystemEventArgs(WatcherChangeTypes.Created, fileSystem.BaseDir, fileName));
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
					Assert.AreEqual(fileSystem.BaseDir.ToLower(), value.ToLower());
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
				if (Renamed != null)
					Renamed(this, null);
			}
		};

		class FileSystemImpl : IFileSystem
		{
			class FileImpl: MemoryStream, IFileStreamInfo
			{
				public DateTime writeTime = new DateTime();
				public int openCounter;
				public bool isDeleted;

				public override void Close()
				{
					Assert.IsTrue(openCounter > 0);
					openCounter--;
				}

				public DateTime LastWriteTime
				{
					get 
					{
						Assert.IsTrue(openCounter > 0);
						return this.writeTime; 
					}
				}

				public bool IsDeleted
				{
					get
					{
						Assert.IsTrue(openCounter > 0);
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
				Assert.IsTrue(files.Remove(name));
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

			public Stream OpenFile(string fileName)
			{
				Assert.AreEqual(baseDir.ToLower(), Path.GetDirectoryName(fileName).ToLower());
				FileImpl ret;
				if (!files.TryGetValue(Path.GetFileName(fileName).ToLower(), out ret))
					throw new FileNotFoundException();
				ret.openCounter++;
				return ret;
			}

			public static string WildcardToRegex(string pattern)
			{
				return "^" + Regex.Escape(pattern).
					Replace("\\*", ".*").
					Replace("\\?", ".") + "$";
			}

			public string[] GetFiles(string path, string searchPattern)
			{
				Assert.AreEqual(baseDir.ToLower(), path.ToLower());

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
				Assert.AreEqual(baseDir.ToLower(), Path.GetDirectoryName(fileName).ToLower());
				FileImpl f;
				if (!files.TryGetValue(Path.GetFileName(fileName).ToLower(), out f))
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

		class MessagesReader : IPositionedMessagesReader
		{
			ILogMedia media;
			public const int EmptyFileContent = 0;
			public const int InvalidFileContent = 666;

			public MessagesReader(MediaBasedReaderParams readerParams, object fmtInfo)
			{
				this.media = readerParams.Media;
			}

#region IPositionedMessagesReader Members

			public long BeginPosition
			{
				get { return 0; }
			}

			public long EndPosition
			{
				get { return media.Size; }
			}

			public async Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode)
			{
				media.Update();
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

			async ValueTask<int> IPositionedMessagesReader.GetContentsEtag()
			{
				return 0;
			}

			public async Task<IPositionedMessagesParser> CreateParser(CreateParserParams p)
			{
				return new Parser(media);
			}

			public ISearchingParser CreateSearchingParser(CreateSearchingParserParams p)
			{
				return null;
			}

#endregion

#region IDisposable Members

			public void Dispose()
			{
			}

#endregion

			public class Parser : IPositionedMessagesParser
			{
				DateTime time;
				static DateTime startOfTime = new DateTime(2000, 1, 1);
				bool messageRead = false;
				public Parser(ILogMedia media)
				{
					media.DataStream.Position = 0;
					TextReader r = new StreamReader(media.DataStream);
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
				}
				public async ValueTask<IMessage> ReadNext()
				{
					if (messageRead)
						return null;
					messageRead = true;
					return new Message(0, 1, null, new MessageTimestamp(time), StringSlice.Empty, SeverityFlag.Info);
				}
				public async ValueTask<PostprocessedMessage> ReadNextAndPostprocess()
				{
					return new PostprocessedMessage(await ReadNext(), null);
				}
				public async Task Dispose()
				{
				}
			};
		};

		ILogMedia CreateMedia(FileSystemImpl fs)
		{
			var media = new RollingFilesMedia(
				fs,
				@params => new MessagesReader(@params, new StreamBasedFormatInfo(null)), 
				LJTraceSource.EmptyTracer,
				new GenericRollingMediaStrategy(fs.BaseDir)
			);
			return media;
		}

		void CheckMedia(ILogMedia media, string expectedContent)
		{
			media.DataStream.Position = 0;
			StreamReader r = new StreamReader(media.DataStream);
			string actual = r.ReadToEnd();
			Assert.AreEqual(expectedContent, actual);
			Assert.AreEqual(media.Size, (long)expectedContent.Length);
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
