using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		readonly Func<MediaBasedReaderParams, IPositionedMessagesReader> readerCreator;
		readonly IRollingFilesMediaStrategy rollingStrategy;
		readonly string baseDirectory;
		readonly LogMedia.IFileSystemWatcher fsWatcher;
		readonly Dictionary<string, LogPart> parts = new Dictionary<string, LogPart>();
		readonly ConcatReadingStream concatStream;
		readonly ILogSourceThreadsInternal tempThreads;
		bool disposed;
		int folderNeedsRescan;

		public RollingFilesMedia(
			LogMedia.IFileSystem fileSystem,
			Func<MediaBasedReaderParams, IPositionedMessagesReader> readerCreator,
			LJTraceSource traceSource,
			IRollingFilesMediaStrategy rollingStrategy)
		{
			trace = traceSource;
			using (trace.NewFrame)
			{
				if (fileSystem == null)
					throw new ArgumentNullException(nameof(fileSystem));

				this.rollingStrategy = rollingStrategy;
				this.readerCreator = readerCreator;

				try
				{
					this.fileSystem = fileSystem;
					this.baseDirectory = rollingStrategy.BaseDirectory;
					trace.Info("Base file directory: {0}", baseDirectory);

					this.concatStream = new ConcatReadingStream();
					this.tempThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, new ModelThreads(), null);

					this.fsWatcher = fileSystem.CreateWatcher();
					this.fsWatcher.Path = this.baseDirectory;
					this.fsWatcher.Created += new FileSystemEventHandler(fsWatcher_Created);
					this.fsWatcher.Renamed += new RenamedEventHandler(fsWatcher_Renamed);
					this.fsWatcher.EnableRaisingEvents = true;

					trace.Info("Watcher enabled");

					this.folderNeedsRescan = 1;
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

		void fsWatcher_Created(object sender, FileSystemEventArgs e)
		{
			trace.Info("File creation notification: {0}", e.Name);
			FileNotificationHandler(e.FullPath);
		}

		void fsWatcher_Renamed(object sender, RenamedEventArgs e)
		{
			trace.Info("File rename notification. {0}->{1}", e.OldName, e.Name);
			FileNotificationHandler(e.FullPath);
		}

		void FileNotificationHandler(string fullPath)
		{
			if (disposed)
			{
				trace.Warning("Media is disposed (or is being disposed)");
				return;
			}
			if (rollingStrategy.IsFileARolledLog(fullPath))
			{
				Interlocked.Exchange(ref folderNeedsRescan, 1);
				trace.Info("File is a rolled log part. Scheduling rescan.");
			}
			else
			{
				trace.Warning("Not a rolled log file");
			}
		}

		#region ILogMedia Members

		public bool IsAvailable
		{
			get { return true; }
		}

		public async Task Update()
		{
			using (trace.NewFrame)
			{
				CheckDisposed();

				if (Interlocked.CompareExchange(ref folderNeedsRescan, 0, 1) != 0)
				{
					trace.Info("Folder will be rescaned. First, disposing existing parts");
					foreach (var p in parts)
					{
						trace.Info("Disposing: {0}", p.Key);
						p.Value.Dispose();
					}
					parts.Clear();
					trace.Info("Scanning the folder and discovering new parts");
					foreach (string fname in fileSystem.GetFiles(baseDirectory, rollingStrategy.InitialSearchFilter))
					{
						trace.Info("Found: {0}", fname);
						LogPart part = new LogPart(this, Path.GetFileName(fname));
						parts[part.DictionaryKey] = part;
					}
				}

				var partsFailedToUpdate = new List<LogPart>();

				trace.Info("Updating parts");
				foreach (LogPart part in parts.Values)
				{
					trace.Info("Handing {0}", part.DictionaryKey);
					if (!await part.Update())
					{
						trace.Info("The part is not valid anymore. Disposing it");
						part.Dispose();
						partsFailedToUpdate.Add(part);
					}
				}

				if (partsFailedToUpdate.Count > 0)
				{
					trace.Info("Removing parts that failed to update ({0})", partsFailedToUpdate.Count);
					foreach (LogPart part in partsFailedToUpdate)
						parts.Remove(part.DictionaryKey);
				}

				var orderedParts = new List<LogPart>(parts.Values);

				orderedParts.Sort(LogPartsComparer.Instance);

				using (trace.NewNamedFrame("Sorted log parts"))
				{
					foreach (LogPart part in orderedParts)
					{
						trace.Info("Part '{0}'. First message time: {1}", part.DictionaryKey,
							part.FirstMessageTime);
					}
				}

				concatStream.Update(orderedParts.Select(f => f.SimpleMedia.DataStream));
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

			public async Task<bool> Update()
			{
				using (owner.trace.NewNamedFrame("Updating {0}", this.fileName))
				{
					CheckDisposed();

					try
					{
						if (simpleMedia == null)
						{
							owner.trace.Info("SimpleMedia object not created yet. Creating");
							simpleMedia = await SimpleFileMedia.Create(
								owner.fileSystem,
								SimpleFileMedia.CreateConnectionParamsFromFileName(Path.Combine(owner.baseDirectory, FileName))
							);
						}

						owner.trace.Info("Updating simple media");
						await simpleMedia.Update();

						if (!simpleMedia.IsAvailable)
						{
							owner.trace.Info("File is not available (i.e. has been deleted)");
							return false;
						}

						if (firstMessageTime == null)
						{
							owner.trace.Info("First message time is unknown. Calculating it");
							using (IPositionedMessagesReader reader = owner.readerCreator(
									new MediaBasedReaderParams(owner.tempThreads, SimpleMedia)))
							{
								owner.trace.Info("Reader created");

								await reader.UpdateAvailableBounds(false);
								owner.trace.Info("Bounds found");

								IMessage first = await PositionedMessagesUtils.ReadNearestMessage(reader, reader.BeginPosition);
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
