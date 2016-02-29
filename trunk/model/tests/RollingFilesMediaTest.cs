using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using LogJoint.LogMedia;
using LogJoint;
using LogJoint.Log4net;
using System.IO;
using System.Text.RegularExpressions;
using LogJoint.Settings;

namespace LogJointTests
{
	[TestClass()]
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
					wtch.FireChanged(baseDir + "\\" + name);
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
						ret.Add(baseDir + "\\" + fname);
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
				: this("c:\\fake folder that doesn't exist")
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

			public UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode)
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

			public IPositionedMessagesParser CreateParser(CreateParserParams p)
			{
				return new Parser(media);
			}

			public IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p)
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
				public IMessage ReadNext()
				{
					if (messageRead)
						return null;
					messageRead = true;
					return new Content(0, null, new MessageTimestamp(time), StringSlice.Empty, SeverityFlag.Info);
				}
				public PostprocessedMessage ReadNextAndPostprocess()
				{
					return new PostprocessedMessage(ReadNext(), null);
				}
				public void Dispose()
				{
				}
			};
		};

		ILogMedia CreateMedia(FileSystemImpl fs)
		{
			var media = new RollingFilesMedia(fs, 
				typeof(MessagesReader), 
				new StreamBasedFormatInfo(null), 
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

		[TestMethod()]
		public void OpeningFilesThatAreEnumeratedInValidOrder()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "123");
			}
		}

		[TestMethod()]
		public void OpeningFilesThatAreEnumeratedInInvalidOrder()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 2);
			fs.AddFile("a1.log", 1);
			fs.AddFile("a2.log", 4);
			fs.AddFile("a3.log", 3);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "1234");
			}
		}

		[TestMethod()]
		public void InvaidFileInTheGroupMustBeIgnored()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", MessagesReader.InvalidFileContent);
			fs.AddFile("a3.log", 4);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "124");
			}
		}

		[TestMethod()]
		public void EmptyFileInTheGroupMustBeIgnored()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", MessagesReader.EmptyFileContent);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", MessagesReader.EmptyFileContent);
			fs.AddFile("a3.log", 4);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "24");
			}
		}

		[TestMethod()]
		public void LastFileGrowing()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			fs.AddFile("a3.log", 4);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "1234");
				
				fs.SetData("a3.log", 89);
				media.Update();
				CheckMedia(media, "12389");
			}
		}

		[TestMethod()]
		public void LastFileGetsSmaller()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			fs.AddFile("a3.log", 89);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "12389");

				fs.SetData("a3.log", 4);
				media.Update();
				CheckMedia(media, "1234");
			}
		}

		[TestMethod()]
		public void NewLastFileCreated()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "123");

				fs.AddFile("a3.log", 4);
				media.Update();
				CheckMedia(media, "1234");
			}
		}

		[TestMethod()]
		public void NewIntermediateFileCreated()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a3.log", 4);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "124");

				fs.AddFile("a2.log", 3);
				media.Update();
				CheckMedia(media, "1234");
			}
		}

		[TestMethod()]
		public void IntermediateFileDeleted()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "123");

				fs.RemoveFile("a1.log");
				media.Update();
				CheckMedia(media, "13");
			}
		}

		[TestMethod()]
		public void RepeatedCreationNotificationsAreHandledCorrectly()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("a0.log", 1);
			fs.AddFile("a1.log", 2);
			fs.AddFile("a2.log", 3);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "123");

				// Create new file over existing one
				fs.AddFile("a1.log", 4);
				media.Update();

				CheckMedia(media, "134");
			}
		}

		[TestMethod]
		public void FileNamesAreNotCaseSensitive()
		{
			FileSystemImpl fs = new FileSystemImpl();
			fs.AddFile("abc1.log", 1);
			fs.AddFile("abc2.log", 2);
			using (var media = CreateMedia(fs))
			{
				media.Update();
				CheckMedia(media, "12");

				// Create new file over existing one
				fs.AddFile("aBC1.log", 4);
				media.Update();

				CheckMedia(media, "24");
			}
		}


	}


}
