using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;
using System.Diagnostics;

namespace LogJoint.Azure
{
	public class LogProvider : AsyncLogProvider
	{
		public LogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams)
			:
			base(host, factory, connectParams)
		{
			StorageAccount account = new StorageAccount(connectionParams);
			if (account.AccountType == StorageAccount.Type.DevelopmentAccount)
				this.table = AzureDiagnosticLogsTable.CreateDevelopmentTable();
			else
				this.table = new AzureDiagnosticLogsTable(account.ToCloudStorageAccount());

			StartAsyncReader("Azure provider thread: " + connectParams.ToString());
		}

		#region ILogProvider methods

		public override IMessagesCollection LoadedMessages
		{
			get
			{
				CheckDisposed();
				return loadedMessages;
			}
		}

		public override IMessagesCollection SearchResult
		{
			get
			{
				CheckDisposed();
				return searchResult;
			}
		}

		public override void LockMessages()
		{
			CheckDisposed();
			Monitor.Enter(messagesLock);
		}

		public override void UnlockMessages()
		{
			CheckDisposed();
			Monitor.Exit(messagesLock);
		}

		public override TimeSpan TimeOffset
		{
			get
			{
				CheckDisposed();
				return timeOffset;
			}
		}

		#endregion

		protected class MyAlgorithm : AsyncLogProvider.Algorithm
		{
			EntryPartition? firstPKFoundByPrevUpdate;
			EntryPartition? currentAvailbaleRangeBegin;
			EntryPartition? currentAvailbaleRangeEnd;
			int lastAvailableTimeUpdate;

			public MyAlgorithm(LogProvider owner)
				: base(owner)
			{
				this.owner = owner;
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				int currentTimeSinceStart = Environment.TickCount;
				if (incrementalMode && (currentTimeSinceStart - lastAvailableTimeUpdate) < UpdateAvailableTimeNotMoreFrequentlyThan)
					return false;
				lastAvailableTimeUpdate = currentTimeSinceStart;

				EntryPartition? firstPK = AzureDiagnosticsUtils.FindFirstMessagePartitionKey(owner.table);
				EntryPartition? lastPK = firstPK != null ? AzureDiagnosticsUtils.FindLastMessagePartitionKey(owner.table, DateTime.UtcNow) : null;

				if (EntryPartition.Compare(firstPKFoundByPrevUpdate.GetValueOrDefault(), firstPK.GetValueOrDefault()) != 0)
				{
					// The first PK has changed sinse last update which means that log table was overwritten. 
					// Fall to non-incremental mode.
					incrementalMode = false;
				}

				if (!incrementalMode && firstPKFoundByPrevUpdate != null)
				{
					// Reset everything that has been loaded so far
					owner.InvalidateEverythingThatHasBeenLoaded();
				}

				firstPKFoundByPrevUpdate = firstPK;

				currentAvailbaleRangeBegin = firstPK;
				currentAvailbaleRangeEnd = lastPK != null ? lastPK.Value.Advance() : new EntryPartition?();

				if (currentAvailbaleRangeBegin != null && currentAvailbaleRangeEnd != null)
					owner.stats.AvailableTime = DateRange.MakeFromBoundaryValues(
						new DateTime(currentAvailbaleRangeBegin.Value.Ticks, DateTimeKind.Utc).ToLocalTime(),
						new DateTime(currentAvailbaleRangeEnd.Value.Ticks, DateTimeKind.Utc).ToLocalTime());
				else
					owner.stats.AvailableTime = null;

				LogProviderStatsFlag f = LogProviderStatsFlag.AvailableTime;
				if (incrementalMode)
					f |= LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag;
				owner.AcceptStats(f);

				return true;
			}

			protected override object ProcessCommand(Command cmd)
			{
				bool fillRanges = false;
				object retVal = null;

				lock (owner.messagesLock)
				{
					switch (cmd.Type)
					{
						case Command.CommandType.NavigateTo:
							fillRanges = NavigateTo(cmd.Date, cmd.Align);
							break;
						case Command.CommandType.Cut:
							// todo: implement it
							break;
						case Command.CommandType.LoadHead:
							// todo: implement it
							fillRanges = true;
							break;
						case Command.CommandType.LoadTail:
							// todo: implement it
							fillRanges = true;
							break;
						case Command.CommandType.PeriodicUpdate:
							fillRanges = UpdateAvailableTime(true) && owner.stats.AvailableTime.HasValue; // && Cut(owner.stats.AvailableTime.Value);
							break;
						case Command.CommandType.Refresh:
							fillRanges = UpdateAvailableTime(false) && owner.stats.AvailableTime.HasValue; // && Cut(owner.stats.AvailableTime.Value);
							break;
						case Command.CommandType.GetDateBound:
							retVal = GetDateBound(cmd);
							break;
						case Command.CommandType.Search:
							// todo: implement it
							break;
						case Command.CommandType.SetTimeOffset:
							fillRanges = SetTimeOffset(cmd);
							break;
					}
				}

				if (fillRanges)
				{
					FillRanges();
				}

				return retVal;
			}

			private bool SetTimeOffset(Command cmd)
			{
				if (owner.timeOffset != cmd.Offset)
				{
					owner.timeOffset = cmd.Offset;
					UpdateAvailableTime(false);
					return true;
				}
				return false;
			}

			bool Cut(DateRange r)
			{
				return Cut(r.Begin, r.End);
			}

			bool Cut(DateTime d1, DateTime d2)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("d1={0}, d2={1}, stats.LoadedTime={2}", d1, d2, owner.stats.LoadedTime);
					return false;
				}
			}

			bool NavigateTo(DateTime? d, NavigateFlag align)
			{
				using (tracer.NewFrame)
				{
					EntryPartition? newOrigin = null;

					switch (align & NavigateFlag.OriginMask)
					{
						case NavigateFlag.OriginDate:
							// todo: check that date in current available range
							newOrigin = new EntryTimestamp(d.Value).Partition;
							break;
						case NavigateFlag.OriginStreamBoundaries:
							if ((align & NavigateFlag.AlignMask) == NavigateFlag.AlignTop)
								newOrigin = currentAvailbaleRangeBegin;
							else
								newOrigin = currentAvailbaleRangeEnd;
							break;
					}

					if (newOrigin != null)
					{
						// optimization to prevent unneccesary expensive reloading
						if (align == (NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries)
						 && !owner.stats.IsShiftableDown.GetValueOrDefault(true))
						{
							return false;
						}

						origin = newOrigin.Value;
						switch (align & NavigateFlag.AlignMask)
						{
							case NavigateFlag.AlignCenter:
								nrOfMessagesToLoadBeforeOrigin = totalNrOfMessages / 2;
								nrOfMessagesToLoadAfterOrigin = totalNrOfMessages / 2;
								break;
							case NavigateFlag.AlignBottom:
								nrOfMessagesToLoadBeforeOrigin = totalNrOfMessages;
								break;
							case NavigateFlag.AlignTop:
								nrOfMessagesToLoadAfterOrigin = totalNrOfMessages;
								break;
						}
						return true;
					}
					return false;
				}
			}

			void LoadHead(DateTime endDate)
			{
				using (tracer.NewFrame)
				{
				}
			}

			void LoadTail(DateTime beginDate)
			{
				using (tracer.NewFrame)
				{
				}
			}

			void FillRanges()
			{
				using (tracer.NewFrame)
				{
					lock (owner.messagesLock)
					{
						owner.loadedMessages.Clear();
					}
					loadedRange = new FileRange.Range(origin.Ticks, origin.Ticks);

					if (nrOfMessagesToLoadAfterOrigin == 0 && nrOfMessagesToLoadBeforeOrigin == 0)
					{
						ClearLoadedMessagesStats();
					}
					else
					{
						SetLoadingState();
						try
						{
							if (nrOfMessagesToLoadAfterOrigin > 0)
								LoadMessagesAfterOrigin();
							if (nrOfMessagesToLoadBeforeOrigin > 0)
								LoadMessagesBeforeOrigin();
							lock (owner.messagesLock)
								UpdateLoadedTimeStats();
						}
						catch (Exception e)
						{
							SetFailedState(e);
						}
					}
				}
			}

			private void SetLoadingState()
			{
				owner.stats.State = LogProviderState.Loading;
				owner.AcceptStats(LogProviderStatsFlag.State);
			}

			private void SetFailedState(Exception error)
			{
				owner.stats.Error = error;
				owner.stats.State = LogProviderState.LoadError;
				owner.AcceptStats(LogProviderStatsFlag.State);
			}

			private void ClearLoadedMessagesStats()
			{
				owner.stats.MessagesCount = 0;
				owner.stats.LoadedTime = DateRange.MakeEmpty();
				owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount | LogProviderStatsFlag.LoadedTime);
				owner.host.OnLoadedMessagesChanged();
			}

			private void LoadMessagesBeforeOrigin()
			{
				Queue<MessageBase> tmpQueue = new Queue<MessageBase>();
				EntryPartition stepRangeEnd = origin;
				for (int stepIdx = 1; ; )
				{
					EntryPartition stepRangeBegin = EntryPartition.Max(currentAvailbaleRangeBegin.Value, origin.Advance(-stepIdx));
					int nrOfItemsInRange = 0;
					int nrOfItemsInDropped = 0;
					foreach (var m in AzureDiagnosticsUtils.LoadMessagesRange(owner.table, owner.host.Threads, stepRangeBegin, stepRangeEnd, null))
					{
						tmpQueue.Enqueue(m);
						if (tmpQueue.Count > nrOfMessagesToLoadBeforeOrigin)
						{
							tmpQueue.Dequeue();
							++nrOfItemsInDropped;
						}
						++nrOfItemsInRange;
					}
					EntryPartition newBegin = tmpQueue.Count > 0 ? 
						tmpQueue.Select(m => new EntryTimestamp(m.Time).Partition).First() : stepRangeBegin;
					ExtendLoadedRange(newBegin.Ticks, null, tmpQueue);
					nrOfMessagesToLoadBeforeOrigin -= tmpQueue.Count;
					if (nrOfMessagesToLoadBeforeOrigin <= 0)
						break;
					if (EntryPartition.Compare(stepRangeBegin, currentAvailbaleRangeBegin.Value) <= 0)
						break;
					stepRangeEnd = stepRangeBegin;
					tmpQueue.Clear();
					bool increaseStep = nrOfItemsInRange < optimalNrOfEntriesPerRequest;
					if (increaseStep)
					{
						stepIdx *= 2;
					}
				}
			}

			private void LoadMessagesAfterOrigin()
			{
				List<MessageBase> buffer = new List<MessageBase>(bufferSize);
				Action flushBuffer = () =>
				{
					if (buffer.Count == 0)
						return;
					ExtendLoadedRange(null, new EntryTimestamp(buffer.Last().Time).Partition.Advance().Ticks, buffer);
					buffer.Clear();
				};
				foreach (var m in AzureDiagnosticsUtils.LoadMessagesRange(owner.table, owner.host.Threads,
					origin, 
					EntryPartition.MaxValue,
					nrOfMessagesToLoadAfterOrigin))
				{
					buffer.Add(m);
					if (buffer.Count >= bufferSize)
						flushBuffer();
				}
				flushBuffer();
			}

			void ExtendLoadedRange(long? newBegin, long? newEnd, IEnumerable<MessageBase> messages)
			{
				bool messagesChanged = false;
				int newMessagesCount = 0;
				lock (owner.messagesLock)
				{
					if (newBegin == null)
						newBegin = loadedRange.Begin;
					if (newEnd == null)
						newEnd = loadedRange.End;
					owner.loadedMessages.SetActiveRange(newBegin.Value, newEnd.Value);
					loadedRange = new FileRange.Range(newBegin.Value, newEnd.Value);
					using (var currentRange = owner.loadedMessages.GetNextRangeToFill())
					{
						foreach (MessageBase m in messages)
						{
							try
							{
								currentRange.Add(m, false);
								messagesChanged = true;
							}
							catch (MessagesContainers.TimeConstraintViolationException)
							{
								owner.tracer.Warning("Time constraint violation. Message: %s %s", m.Time.ToString(), m.Text);
							}
						}
						if (messagesChanged)
						{
							newMessagesCount = owner.loadedMessages.Count;
						}
					}
				}
				if (messagesChanged)
				{
					owner.stats.MessagesCount = newMessagesCount;
					owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount);
					owner.host.OnLoadedMessagesChanged();
				}
			}

			DateBoundPositionResponseData GetDateBound(Command cmd)
			{
				DateBoundPositionResponseData ret = new DateBoundPositionResponseData();

				CancellationTokenSource cancellation = new CancellationTokenSource();
				
				owner.SetCurrentCommandCancellation(cancellation);
				try
				{
					var stopwatch = Stopwatch.StartNew();
					var boundEntry = AzureDiagnosticsUtils.FindDateBound(owner.table, cmd.Date.Value, cmd.Bound, currentAvailbaleRangeBegin.Value,
						currentAvailbaleRangeEnd.Value, cancellation.Token);
					Debug.WriteLine("GetDataBound({0} {4}, {1}) took {2} -> {3}", cmd.Date.Value, cmd.Bound, stopwatch.Elapsed,
						boundEntry.HasValue ? boundEntry.Value.Entry.EventTickCount.ToString() : "null", cmd.Date.Value.ToUniversalTime().Ticks);
					if (boundEntry == null)
					{
						if (cmd.Bound == PositionedMessagesUtils.ValueBound.Lower)
						{
							ret.IsEndPosition = true;
							ret.Position = currentAvailbaleRangeEnd.Value.Ticks;
						}
						else if (cmd.Bound == PositionedMessagesUtils.ValueBound.LowerReversed)
						{
							ret.IsBeforeBeginPosition = true;
							ret.Position = currentAvailbaleRangeBegin.Value.Ticks - 1;
						}
					}
					else
					{
						ret.Date = new MessageTimestamp(new DateTime(boundEntry.Value.Entry.EventTickCount, DateTimeKind.Utc));
						ret.Position = new EntryPartition(boundEntry.Value.Entry.EventTickCount).MakeMessagePosition(boundEntry.Value.IndexWithinPartition);
					}
				}
				catch (OperationCanceledException)
				{
					return null;
				}
				finally
				{
					owner.SetCurrentCommandCancellation(null);
				}
				return ret;
			}

			void UpdateLoadedTimeStats()
			{
				MessagesContainers.Messages tmp = owner.loadedMessages;

				int c = tmp.Count;
				if (c != 0)
				{
					DateTime begin = tmp.Forward(0, 1).First().Message.Time.ToLocalDateTime();
					DateTime end = tmp.Reverse(c - 1, c - 2).First().Message.Time.ToLocalDateTime();
					owner.stats.LoadedTime = DateRange.MakeFromBoundaryValues(begin, end);
				}
				else
				{
					owner.stats.LoadedTime = DateRange.MakeEmpty();
				}
				owner.stats.IsFullyLoaded = tmp.Count >= MyAlgorithm.totalNrOfMessages;
				owner.stats.IsShiftableDown = tmp.ActiveRange.End < currentAvailbaleRangeEnd.Value.Ticks;
				owner.stats.IsShiftableUp = tmp.ActiveRange.Begin > currentAvailbaleRangeBegin.Value.Ticks;

				owner.AcceptStats(LogProviderStatsFlag.LoadedTime);
			}

			readonly LogProvider owner;
			
			EntryPartition origin;
			int nrOfMessagesToLoadBeforeOrigin;
			int nrOfMessagesToLoadAfterOrigin;
			FileRange.Range loadedRange;
			
			const int UpdateAvailableTimeNotMoreFrequentlyThan = 1000 * 30; // millisecs
			internal const int totalNrOfMessages = 300;
			const int optimalNrOfEntriesPerRequest = 50;
			const int bufferSize = 100;
		};

		protected void InvalidateMessages()
		{
			using (tracer.NewFrame)
			{
				if (IsDisposed)
					return;
				lock (messagesLock)
					loadedMessages.InvalidateMessages();
			}
		}

		protected void InvalidateSearchResults()
		{
		}

		protected override Algorithm CreateAlgorithm()
		{
			return new MyAlgorithm(this);
		}

		protected override void InvalidateEverythingThatHasBeenLoaded()
		{
			lock (messagesLock)
			{
				InvalidateMessages();
				InvalidateSearchResults();
				base.InvalidateEverythingThatHasBeenLoaded();
			}
		}

		readonly object messagesLock = new object();
		readonly IAzureDiagnosticLogsTable table;
		TimeSpan timeOffset;
		MessagesContainers.Messages loadedMessages = new MessagesContainers.Messages();
		MessagesContainers.Messages searchResult = new MessagesContainers.Messages();
	}

	public class StorageAccount
	{
		public StorageAccount(IConnectionParams connectParams)
		{
			var name = connectParams["name"];
			if (name == DevelopmentAccountName)
			{
				type = Type.DevelopmentAccount;
				this.name = DevelopmentAccountName;
			}
			else
			{
				type = Type.CloudAccount;
				this.name = name;
				this.key = connectParams["key"];
				this.useHttps = bool.Parse(connectParams["useHttps"]);
			}
		}
		public StorageAccount()
		{
			this.type = Type.DevelopmentAccount;
			this.name = DevelopmentAccountName;
		}
		public StorageAccount(string name, string key, bool useHttps)
		{
			this.type = Type.CloudAccount;
			this.name = name;
			this.key = key;
			this.useHttps = useHttps;
		}

		public override string ToString()
		{
			return ToConnectionString();
		}

		public string ToUserFriendlyString()
		{
			string userFriendlyName;
			if (type == Type.DevelopmentAccount)
				userFriendlyName = "development account";
			else
				userFriendlyName = name;
			return "Azure Diagnostics Log (" + userFriendlyName + ")";
		}

		public string ToConnectionString()
		{
			switch (type)
			{
				case Type.DevelopmentAccount:
					return DevelopmentAccountName;
				case Type.CloudAccount:
					return string.Format(
						"DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}",
						useHttps ? "https" : "http", name, key);
				default:
					return "";
			}
		}

		public CloudStorageAccount ToCloudStorageAccount()
		{
			return CloudStorageAccount.Parse(ToConnectionString());
		}

		public static readonly string DevelopmentAccountName = "UseDevelopmentStorage=true";

		public enum Type
		{
			DevelopmentAccount,
			CloudAccount
		};

		public Type AccountType { get { return type; } }
		public string AccountName { get { return name; } }
		public string AccountKey { get { return key; } }
		public bool UseHTPPS { get { return useHttps; } }

		public void SaveToConnectionParams(IConnectionParams connectParams)
		{
			switch (type)
			{
				case Type.DevelopmentAccount:
					connectParams["name"] = DevelopmentAccountName;
					break;
				case Type.CloudAccount:
					connectParams["name"] = name;
					connectParams["key"] = key;
					connectParams["useHttps"] = useHttps.ToString();
					break;
			}
			connectParams[ConnectionParamsUtils.IdentityConnectionParam] = string.Format("wad-{0}", name);
		}

		Type type;
		string name;
		string key;
		bool useHttps;
	};

	public class Factory : ILogProviderFactory
	{
		public static readonly Factory Instance = new Factory();

		static Factory()
		{
			LogProviderFactoryRegistry.DefaultInstance.Register(Instance);
		}

		public IConnectionParams CreateParams(StorageAccount account)
		{
			var ret = new ConnectionParams();
			account.SaveToConnectionParams(ret);
			return ret;
		}

		#region ILogReaderFactory Members

		public string CompanyName
		{
			get { return "Microsoft"; }
		}

		public string FormatName
		{
			get { return "Azure Diagnostics Log"; }
		}

		public string FormatDescription
		{
			get { return "Windows Azure Diagnostics log that is stored in Azure Tables Storage table (WADLogsTable)"; }
		}

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return new FactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return new StorageAccount(connectParams).ToUserFriendlyString();
		}

		public string GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return originalConnectionParams.Clone(true);
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, this, connectParams);
		}

		public IFormatViewOptions ViewOptions { get { return FormatViewOptions.NoRawView; } }

		public LogFactoryFlag Flags
		{
			get
			{
				return LogFactoryFlag.None;
			}
		}

		#endregion
	};
}
