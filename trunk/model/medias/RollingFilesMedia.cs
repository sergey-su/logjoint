using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint
{
	public interface IRollingFilesMediaStrategy
	{
		string BaseDirectory { get; }
		string InitialSearchFilter { get; }
		bool IsFileARolledLog(string fileNameToTest);
	};

	public class RollingFilesMedia : ILogMedia
	{
		readonly LJTraceSource trace = LJTraceSource.EmptyTracer;
		readonly LogMedia.IFileSystem fileSystem;
		readonly StreamBasedMediaInitParams initParams;
		readonly IRollingFilesMediaStrategy rollingStrategy;
		readonly string baseDirectory;
		readonly LogMedia.IFileSystemWatcher fsWatcher;
		readonly Dictionary<string, LogPart> parts = new Dictionary<string, LogPart>();
		readonly List<LogPart> tempList = new List<LogPart>();
		readonly List<LogPart> dynamicallyChangedParts = new List<LogPart>();
		readonly ConcatReadingStream concatStream;
		readonly LogSourceThreads tempThreads;
		bool disposed;

		public RollingFilesMedia(
			LogMedia.IFileSystem fileSystem, 
			IConnectionParams connectParams, 
			MediaInitParams p,
			IRollingFilesMediaStrategy rollingStrategy)
		{
			trace = p.Trace;
			using (trace.NewFrame)
			{
				if (fileSystem == null)
					throw new ArgumentNullException("fileSystem");
				if (connectParams == null)
					throw new ArgumentNullException("connectParams");

				this.rollingStrategy = rollingStrategy;

				initParams = p as StreamBasedMediaInitParams;
				if (initParams == null)
					throw new ArgumentException("Init parameters of invalid type passed");

				try
				{
					this.fileSystem = fileSystem;
					this.baseDirectory = rollingStrategy.BaseDirectory;
					trace.Info("Base file directory: {0}", baseDirectory);

					this.concatStream = new ConcatReadingStream();
					this.tempThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, new Threads(), null);

					this.fsWatcher = fileSystem.CreateWatcher();
					this.fsWatcher.Path = this.baseDirectory;
					this.fsWatcher.Created += new FileSystemEventHandler(fsWatcher_Created);
					this.fsWatcher.Renamed += new RenamedEventHandler(fsWatcher_Renamed);
					this.fsWatcher.EnableRaisingEvents = true;

					trace.Info("Watcher enabled");

					InitialSearchForFiles();
				}
				catch
				{
					trace.Error("Initialization failed. Disposing.");
					Dispose();
					throw;
				}
			}
		}

		protected IRollingFilesMediaStrategy RollingStrategy { get { return rollingStrategy; } }

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		static char GetFileNameFirstChar(string path)
		{
			string fileName = Path.GetFileName(path);
			if (fileName.Length == 0)
				throw new ArgumentException("path is invalid");
			return fileName[0];
		}

		void InitialSearchForFiles()
		{
			using (trace.NewFrame)
			{
				foreach (string fname in fileSystem.GetFiles(baseDirectory, rollingStrategy.InitialSearchFilter))
				{
					trace.Info("Found: {0}", fname);
					LogPart part = new LogPart(this, Path.GetFileName(fname));
					parts[part.DictionaryKey] = part;
				}
			}
		}

		void fsWatcher_Created(object sender, FileSystemEventArgs e)
		{
			using (trace.NewFrame)
			{
				trace.Info("Full file path: {0}", e.FullPath);
				if (disposed)
				{
					trace.Warning("Media is disposed (or is being disposed)");
					return;
				}
				if (rollingStrategy.IsFileARolledLog(e.FullPath))
				{
					LogPart part = new LogPart(this, e.Name);
					lock (dynamicallyChangedParts)
					{
						dynamicallyChangedParts.Add(part);
						trace.Info("Added new dinamically changed part. Count={0}", dynamicallyChangedParts.Count.ToString());
					}
				}
				else
				{
					trace.Warning("Not a rolled log file");
				}
			}
		}

		void fsWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			using (trace.NewFrame)
			{
				fsWatcher_Created(sender, e);
			}
		}

		static IEnumerable<Stream> EnumStreams(List<LogPart> list)
		{
			foreach (LogPart f in list)
				yield return f.SimpleMedia.DataStream;
		}

		#region ILogMedia Members

		public bool IsAvailable
		{
			get { return true; }
		}

		public void Update()
		{
			using (trace.NewFrame)
			{
				CheckDisposed();

				// last file grows: update last file
				// new last file craeted: get notif from watcher, update new last and prev last. Full rescan occasionaly.
				// old files deleted: rescan with update of all files occasionally.

				lock (dynamicallyChangedParts)
				{
					if (dynamicallyChangedParts.Count > 0)
					{
						trace.Info("Amount of dynamically changed parts: {0}", dynamicallyChangedParts.Count);
						foreach (LogPart p in dynamicallyChangedParts)
						{
							trace.Info("Handling {0}", p.FileName);
							LogPart existingPart;
							if (!parts.TryGetValue(p.DictionaryKey, out existingPart))
							{
								trace.Info("Part with key '{0}' doesn't exist", p.DictionaryKey);
								parts.Add(p.DictionaryKey, p);
							}
							else
							{
								trace.Info("Part with key '{0}' already exists. Invalidating it.", p.DictionaryKey);
								p.Dispose();
								existingPart.InvalidateFirstMessageTime();
							}
						}
						dynamicallyChangedParts.Clear();
					}
				}

				tempList.Clear();

				trace.Info("Updating parts");
				foreach (LogPart part in parts.Values)
				{
					trace.Info("Handing {0}", part.DictionaryKey);
					if (!part.Update())
					{
						trace.Info("The part is not valid anymore. Disposing it");
						part.Dispose();
						tempList.Add(part);
					}
				}

				if (tempList.Count > 0)
				{
					trace.Info("Removing disposed parts ({0})", tempList.Count);
					foreach (LogPart part in tempList)
					{
						parts.Remove(part.DictionaryKey);
					}
				}

				tempList.Clear();
				tempList.AddRange(parts.Values);
				tempList.Sort(LogPartsComparer.Instance);

				using (trace.NewNamedFrame("Parts"))
				{
					foreach (LogPart part in tempList)
					{
						trace.Info("Part '{0}'. First message time: {1}", part.DictionaryKey,
							part.FirstMessageTime);
					}
				}

				concatStream.Update(EnumStreams(tempList));
			}
		}

		public Stream DataStream
		{
			get
			{
				CheckDisposed();
				return concatStream;
			}
		}

		public DateTime LastModified
		{
			get
			{
				CheckDisposed();
				return DateTime.MinValue;
			}
		}

		public long Size
		{
			get
			{
				CheckDisposed();
				return concatStream.Length;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			using (trace.NewFrame)
			{
				if (!disposed)
				{
					disposed = true;
					if (fsWatcher != null)
					{
						fsWatcher.EnableRaisingEvents = false;
					}
					if (tempThreads != null)
					{
						tempThreads.Dispose();
					}
					using (trace.NewNamedFrame("Disposing parts"))
					{
						foreach (LogPart part in parts.Values)
						{
							trace.Info("Handling {0}", part.DictionaryKey);
							part.Dispose();
						}
					}
					if (concatStream != null)
					{
						concatStream.Dispose();
					}
					trace.Info("Disposing watcher");
					if (fsWatcher != null)
					{
						fsWatcher.Dispose();
					}
				}
			}
		}

		#endregion

		class LogPart : IDisposable
		{
			private readonly RollingFilesMedia owner;
			readonly string fileName;
			MessageTimestamp? firstMessageTime;
			SimpleFileMedia simpleMedia;
			bool isDisposed;

			public LogPart(RollingFilesMedia owner, string fileName)
			{
				this.owner = owner;
				this.fileName = fileName;
			}

			public bool IsDisposed
			{
				get { return isDisposed; }
			}

			public bool Update()
			{
				using (owner.trace.NewNamedFrame("Updating {0}", this.fileName))
				{
					CheckDisposed();

					try
					{

						if (simpleMedia == null)
						{
							owner.trace.Info("SimpleMedia object not created yet. Creating");
							simpleMedia = new SimpleFileMedia(
								owner.fileSystem,
								SimpleFileMedia.CreateConnectionParamsFromFileName(owner.baseDirectory + "\\" + FileName),
								new MediaInitParams(owner.trace)
							);
						}

						owner.trace.Info("Updating simple media");
						simpleMedia.Update();

						if (!simpleMedia.IsAvailable)
						{
							owner.trace.Info("File is not avaliable (i.e. has been deleted)");
							return false;
						}

						if (firstMessageTime == null)
						{
							owner.trace.Info("First message time is unknown. Calcalating it");
							using (IPositionedMessagesReader reader = (IPositionedMessagesReader)Activator.CreateInstance(
									owner.initParams.ReaderType, new MediaBasedReaderParams(owner.tempThreads, SimpleMedia), owner.initParams.FormatInfo))
							{
								owner.trace.Info("Reader created");

								reader.UpdateAvailableBounds(false);
								owner.trace.Info("Bounds found");

								MessageBase first = PositionedMessagesUtils.ReadNearestMessage(reader, reader.BeginPosition);
								if (first == null)
								{
									owner.trace.Warning("No messages found");
									return false;
								}

								owner.trace.Info("First message: {0} '{1}'", first.Time, first.Text);
								firstMessageTime = first.Time;
							}
						}

						owner.trace.Info("Part updated OK");
						return true;
					}
					catch (Exception e)
					{
						owner.trace.Error(e, "Failure during part update");
						return false;
					}
				}
			}

			public string FileName
			{
				get
				{
					CheckDisposed();
					return fileName;
				}
			}

			public string DictionaryKey
			{
				get
				{
					return fileName.ToLower();
				}
			}

			public MessageTimestamp FirstMessageTime
			{
				get
				{
					CheckDisposed();
					if (!firstMessageTime.HasValue)
						throw new InvalidOperationException("The time of the first message is not defined. Call Update() first.");
					return firstMessageTime.Value;
				}
			}

			public SimpleFileMedia SimpleMedia
			{
				get
				{
					CheckDisposed();
					return simpleMedia;
				}
			}

			public void InvalidateFirstMessageTime()
			{
				owner.trace.Info("Invalidating first message time for {0}", DictionaryKey);
				firstMessageTime = null;
			}

			#region IDisposable Members

			public void Dispose()
			{
				using (owner.trace.NewNamedFrame("Disposing {0}", DictionaryKey))
				{
					if (isDisposed)
					{
						owner.trace.Warning("Already disposed");
						return;
					}
					isDisposed = true;
					if (simpleMedia != null)
					{
						owner.trace.Info("Disposing SimpleMedia object");
						simpleMedia.Dispose();
					}
				}
			}

			#endregion

			void CheckDisposed()
			{
				if (isDisposed)
				{
					owner.trace.Warning("Part disposed");
					throw new ObjectDisposedException("LogPart " + fileName);
				}
			}
		};

		class LogPartsComparer : IComparer<LogPart>
		{
			public int Compare(LogPart x, LogPart y)
			{
				int tmp;

				tmp = MessageTimestamp.Compare(x.FirstMessageTime, y.FirstMessageTime);
				if (tmp != 0)
					return tmp;

				tmp = string.Compare(x.FileName, y.FileName, true);
				if (tmp != 0)
					return tmp;

				return tmp;
			}

			public static readonly LogPartsComparer Instance = new LogPartsComparer();
		};
	}
}
