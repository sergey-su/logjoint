using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace LogJoint
{
	public abstract class RangeManagingProvider : AsyncLogProvider
	{
		public RangeManagingProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams)
			:
			base(host, factory, connectParams)
		{
		}

		public override ITimeOffsets TimeOffsets
		{
			get
			{
				CheckDisposed();
				return GetReader().TimeOffsets;
			}
		}

		protected class RangeManagingAlgorithm : AsyncLogProvider.Algorithm
		{
			IMessage firstMessage;
			bool firstUpdateFlag = true;

			public RangeManagingAlgorithm(RangeManagingProvider owner, IPositionedMessagesReader reader)
				: base(owner)
			{
				this.owner = owner;
				this.reader = reader;
			}

			private static DateRange? GetAvailableDateRangeHelper(IMessage first, IMessage last)
			{
				if (first == null || last == null)
					return null;
				return DateRange.MakeFromBoundaryValues(first.Time.ToLocalDateTime(), last.Time.ToLocalDateTime());
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				bool itIsFirstUpdate = firstUpdateFlag;
				firstUpdateFlag = false;

				UpdateBoundsStatus status = reader.UpdateAvailableBounds(incrementalMode);

				if (status == UpdateBoundsStatus.NothingUpdated && incrementalMode)
				{
					return false;
				}

				if (status == UpdateBoundsStatus.OldMessagesAreInvalid)
				{
					incrementalMode = false;
				}

				// Get new boundary values into temporary variables
				IMessage newFirst, newLast;
				PositionedMessagesUtils.GetBoundaryMessages(reader, null, out newFirst, out newLast);

				if (firstMessage != null)
				{
					if (newFirst == null || MessageTimestamp.Compare(newFirst.Time, firstMessage.Time) != 0)
					{
						// The first message we've just read differs from the cached one. 
						// This means that the log was overwritten. Fall to non-incremental mode.
						incrementalMode = false;
					}
				}

				if (!incrementalMode)
				{
					if (!itIsFirstUpdate)
					{
						// Reset everything that has been loaded so far
						owner.InvalidateEverythingThatHasBeenLoaded();
					}
					firstMessage = null;
				}

				// Try to get the dates range for new bounday messages
				DateRange? newAvailTime = GetAvailableDateRangeHelper(newFirst, newLast);
				firstMessage = newFirst;

				// Getting here means that the boundaries changed. 
				// Fire the notfication.

				owner.stats.AvailableTime = newAvailTime;
				LogProviderStatsFlag f = LogProviderStatsFlag.AvailableTime;
				if (incrementalMode)
					f |= LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag;
				owner.stats.TotalBytes = reader.SizeInBytes;
				f |= LogProviderStatsFlag.BytesCount;
				owner.stats.PositionsRange = new FileRange.Range(reader.BeginPosition, reader.EndPosition);
				f |= LogProviderStatsFlag.PositionsRange;

				owner.AcceptStats(f);

				return true;
			}

			protected override object ProcessCommand(Command cmd)
			{
				switch (cmd.Type)
				{
					case Command.CommandType.PeriodicUpdate:
						UpdateAvailableTime(true);// && owner.stats.AvailableTime.HasValue && Cut(owner.stats.AvailableTime.Value);
						break;
					case Command.CommandType.Refresh:
						owner.RefreshHook();
						UpdateAvailableTime(false);// && owner.stats.AvailableTime.HasValue && Cut(owner.stats.AvailableTime.Value);
						break;
					case Command.CommandType.GetDateBound:
						if (owner.stats.LoadedTime.IsInRange(cmd.Date.Value))
						{
							// todo: optimize the command for the case when the message with cmd.Date is loaded in memory
						}
						tracer.Info("Date bound was not found among messages in the memory. Looking in the log media");
						return GetDateBoundFromMedia(cmd);
						break;
					case Command.CommandType.Search:
						/*
						bool isFullyLoaded =
							owner.loadedMessages.ActiveRange.End >= reader.EndPosition
						 && owner.loadedMessages.ActiveRange.Begin <= reader.BeginPosition;
						if (isFullyLoaded)
						{
							retVal = SearchSynchronously(cmd.SearchParams);
						}*/
						Search(cmd.SearchParams, cmd.Cancellation, cmd.Callback);
						break;
					case Command.CommandType.SetTimeOffset:
						if (!cmd.TimeOffsets.Equals(reader.TimeOffsets))
						{
							reader.TimeOffsets = cmd.TimeOffsets;
							UpdateAvailableTime(false);
							// todo: invalidate cache
						}
						break;
					case Command.CommandType.Get:
						EnumMessages(cmd.StartFrom, cmd.Flags, cmd.Callback, cmd.Cancellation);
						break;
					case Command.CommandType.UpdateCache:
						var currentRange = owner.messagesCache.ActiveRange;
						long cacheSize = 4*1024*1024;  // todo: use configuration
						bool moveCacheRange = currentRange.IsEmpty || 
							Math.Abs((currentRange.Begin + currentRange.End) / 2 - cmd.StartFrom) > cacheSize / 6;
						if (moveCacheRange)
						{
							if (ConstrainedNavigate(
								cmd.StartFrom - cacheSize/2,
								cmd.StartFrom + cacheSize/2
							))
							{
								FillCacheRanges();
							}
						}
						break;
				}
				return null;
			}

			DateBoundPositionResponseData GetDateBoundFromMedia(Command cmd)
			{
				DateBoundPositionResponseData ret = new DateBoundPositionResponseData();
				ret.Position = PositionedMessagesUtils.LocateDateBound(reader, cmd.Date.Value, cmd.Bound, cmd.Cancellation);
				tracer.Info("Position to return: {0}", ret.Position);

				if (ret.Position == reader.EndPosition)
				{
					ret.IsEndPosition = true;
					tracer.Info("It is END position");
				}
				else if (ret.Position == reader.BeginPosition - 1)
				{
					ret.IsBeforeBeginPosition = true;
					tracer.Info("It is BEGIN-1 position");
				}
				else
				{
					cmd.Cancellation.ThrowIfCancellationRequested();
					ret.Date = PositionedMessagesUtils.ReadNearestMessageTimestamp(reader, ret.Position);
					tracer.Info("Date to return: {0}", ret.Date);
				}

				return ret;
			}

			void ResetFlags()
			{
				breakAlgorithm = false;
				loadingInterrupted = false;
				lastReadMessage = null;
			}

			bool FlushBuffer(bool reallocateMessageBuffers = false)
			{
				if (readBuffer.Count == 0)
					return false;

				bool messagesChanged = false;
				int newMessagesCount = 0;
				IMessage firstMessageWithTimeConstraintViolation = null;

				if (reallocateMessageBuffers)
				{
					ReallocateMessageBuffers();
				}

				lock (owner.messagesCacheLock)
				{
					foreach (IMessage m in readBuffer)
					{
						try
						{
							currentRange.Add(m, false);
							messagesChanged = true;
						}
						catch (TimeConstraintViolationException)
						{
							owner.tracer.Warning("Time constraint violation. Message: %s %s", m.Time.ToString(), m.Text);
							if (firstMessageWithTimeConstraintViolation == null)
								firstMessageWithTimeConstraintViolation = m;
						}
					}
					if (messagesChanged)
					{
						newMessagesCount = currentMessagesContainer.Count;
					}
				}
				readBuffer.Clear();
				if (messagesChanged)
				{
					if (currentMessagesContainer == owner.messagesCache)
					{
						owner.stats.MessagesCount = newMessagesCount;
						owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount);
					}
				}
				if (firstMessageWithTimeConstraintViolation != null
				 && owner.stats.FirstMessageWithTimeConstraintViolation == null)
				{
					owner.stats.FirstMessageWithTimeConstraintViolation = firstMessageWithTimeConstraintViolation;
					owner.AcceptStats(LogProviderStatsFlag.FirstMessageWithTimeConstraintViolation);
				}

				return true;
			}

			private void ReallocateMessageBuffers()
			{
				var buffer = new StringBuilder(readBuffer.Aggregate(0, (l, m) => l + m.Text.Length));
				readBuffer.Aggregate(buffer, (buf, m) => m.Text.Append(buf));
				string bufferStr = buffer.ToString();
				readBuffer.Aggregate(0, (pos, m) => m.ReallocateTextBuffer(bufferStr, pos));
			}

			private void ReportLoadErrorIfAny()
			{
				if (loadError != null)
				{
					owner.stats.Error = loadError;
					owner.stats.State = LogProviderState.LoadError;
					owner.AcceptStats(LogProviderStatsFlag.State | LogProviderStatsFlag.Error);
				}
			}

			private bool ProcessLastReadMessageAndFlush(bool reallocateMessageBuffers = false)
			{
				if (lastReadMessage != null)
				{
					readBuffer.Add(lastReadMessage);

					if (readBuffer.Count >= 1024)
					{
						FlushBuffer(reallocateMessageBuffers);
						return true;
					}
				}
				return false;
			}

			bool ConstrainedNavigate(long p1, long p2)
			{
				if (p1 < reader.BeginPosition)
				{
					p2 = Math.Min(reader.EndPosition, p2 + reader.BeginPosition - p1);
					p1 = reader.BeginPosition;
				}
				if (p2 >= reader.EndPosition)
				{
					p1 = Math.Max(reader.BeginPosition, p1 - p2 + reader.EndPosition);
					p2 = reader.EndPosition;
				}

				//var pos1 = PositionedMessagesUtils.NormalizeMessagePosition(reader, p1);
				//var pos2 = PositionedMessagesUtils.NormalizeMessagePosition(reader, p2);
				var pos1 = p1;
				var pos2 = p2;

				tracer.Info("setting new active range {0}-{1} (aligned {2}-{3})", p1, p2, pos1, pos2);
				tracer.Info("messages before changing the active range: {0}", owner.messagesCache);

				if (owner.messagesCache.SetActiveRange(pos1, pos2))
				{
					tracer.Info("messages changed. new messages: {0}", owner.messagesCache);

					owner.stats.MessagesCount = owner.messagesCache.Count;
					owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount);
					return true;
				}
				else
				{
					tracer.Info("setting a new active range didn't make any change in messages");
					return false;
				}
			}

			void EnumMessages(long startFrom, EnumMessagesFlag flags, Func<IMessage, bool> callback, CancellationToken cancellation)
			{
				// todo: handle synchroniously enumeration of messages from position smaller than begin/larger than end
				bool finishedSynchroniously = false;
				long positionToContinueAsync = startFrom;
				var direction = (flags & EnumMessagesFlag.Backward) != 0 ? 
					MessagesParserDirection.Backward : MessagesParserDirection.Forward;
				foreach (var r in owner.messagesCache.Ranges.Where(
					r => r.IsComplete && r.LoadedRange.IsInRange(startFrom + (direction == MessagesParserDirection.Forward ? 0 : -1))))
				{
					foreach (var i in (direction == MessagesParserDirection.Forward ? r.Forward(startFrom) : r.Reverse(startFrom)))
					{
						finishedSynchroniously = !callback(i);
						if (finishedSynchroniously)
							break;
						positionToContinueAsync = i.Position + (direction == MessagesParserDirection.Forward ? 1 : -1);
					}
					break;
				}
				if (finishedSynchroniously) // todo: run sync part w/o posting to commands queue
					return;

				var parserFlags = (flags & EnumMessagesFlag.IsSequentialScanningHint) != 0 ? MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading : MessagesParserFlag.None;
				using (var parser = reader.CreateParser(new CreateParserParams(positionToContinueAsync, null, parserFlags, direction)))
				{
					for (;;)
					{
						cancellation.ThrowIfCancellationRequested();
						var m = parser.ReadNext();
						if (m == null)
							break;
						if (!callback(m))
							break;
					}
				}
			}

			void FillCacheRanges()
			{
				using (tracer.NewFrame)
				using (var perfop = new Profiling.Operation(tracer, "FillRanges"))
				{
					bool updateStarted = false;
					try
					{
						// Iterate through the ranges
						for (; ; )
						{
							lock (owner.messagesCacheLock)
							{
								currentRange = owner.messagesCache.GetNextRangeToFill();
								if (currentRange == null) // Nothing to fill
								{
									break;
								}
								currentMessagesContainer = owner.messagesCache;
								tracer.Info("currentRange={0}", currentRange);
							}

							try
							{
								if (!updateStarted)
								{
									tracer.Info("Starting to update the messages.");

									owner.stats.State = LogProviderState.Loading;
									owner.AcceptStats(LogProviderStatsFlag.State);

									updateStarted = true;
								}

#if !SILVERLIGHT
								Stopwatch stopWatch = new Stopwatch();
								stopWatch.Start();
#endif
								long messagesRead = 0;

								ResetFlags();

								// Start reading elements
								using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(
										currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange,
										MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading, 
										MessagesParserDirection.Forward)))
								{
									tracer.Info("parser created");
									for (; ; )
									{
										ResetFlags();

										ReadNextMessage(parser);

										ProcessLastReadMessageAndFlush();

										ReportLoadErrorIfAny();

										if (breakAlgorithm)
										{
											break;
										}

										++messagesRead;
									}
								}

								tracer.Info("reading finished");

								FlushBuffer();

#if !SILVERLIGHT
								stopWatch.Stop();
								if (messagesRead > 0)
								{
									TimeSpan aveMsgTime = new TimeSpan(stopWatch.ElapsedTicks / messagesRead);
									owner.stats.AvePerMsgTime = aveMsgTime;
									owner.AcceptStats(LogProviderStatsFlag.AveMsgTime);
									string str = aveMsgTime.ToString();
									tracer.Info("Average message time={0}", str);
								}
#endif
							}
							finally
							{
								lock (owner.messagesCacheLock)
								{
									if (!loadingInterrupted)
									{
										tracer.Info("Loading of the range finished completly. Completing the range.");
										currentRange.Complete();
										tracer.Info("Disposing the range.");
									}
									else
									{
										tracer.Info("Loading was interrupted. Disposing the range without completion.");
									}
									currentRange.Dispose();
									currentRange = null;
									currentMessagesContainer = null;
								}
								perfop.Milestone("range completed");
							}

							if (loadingInterrupted)
							{
								tracer.Info("Loading interrupted. Stopping to read ranges.");
								break;
							}
						}

						lock (owner.messagesCacheLock)
						{
							owner.UpdateLoadedTimeStats(reader);
						}
					}
					finally
					{
						if (updateStarted)
						{
							tracer.Info("Finishing to update the messages.");
						}
						else
						{
							tracer.Info("There were no ranges to read");
						}
					}
				}
			}

			void ReadNextMessage(IPositionedMessagesParser parser)
			{
				try
				{
					var tmp = parser.ReadNextAndPostprocess();
					lastReadMessage = tmp.Message;
					if (lastReadMessage == null)
					{
						breakAlgorithm = true;
					}
				}
				catch (Exception e)
				{
					loadError = e;
					breakAlgorithm = true;
				}
			}

			void Search(
				SearchAllOccurencesParams searchParams,
				CancellationToken cancellation,
				Func<IMessage, bool> callback
			)
			{
				var searchRange = new FileRange.Range(
					reader.BeginPosition, reader.EndPosition);

				var parserParams = new CreateSearchingParserParams()
				{
					Range = searchRange,
					SearchParams = searchParams,
					Cancellation = cancellation,
					ProgressHandler = (pos) =>
					{
						UpdateSearchCompletionPercentage(pos, searchRange, false);
					}
				};

				using (var parser = reader.CreateSearchingParser(parserParams))
				{
					for (; ; )
					{
						var msg = parser.ReadNext();
						if (msg == null || !callback(msg))
							break;
						cancellation.ThrowIfCancellationRequested();
					}
				}

				SetFinalSearchPercentageValue();
			}

			/*
			SearchAllOccurencesResponseData SearchSynchronously(SearchAllOccurencesParams searchParams)
			{
				owner.stats.SearchResultMessagesCount = 0;
				int maxHitsCount = owner.host.GlobalSettings.MaxNumberOfHitsInSearchResultsView;

				var response = new SearchAllOccurencesResponseData();
				var preprocessedSearchOptions = searchParams.Options.TryPreprocess();
				if (preprocessedSearchOptions != null)
				{
					var bulkSearchState = new Search.BulkSearchState();
					using (var currentRange = owner.searchResult.GetNextRangeToFill())
					using (var threadsBulkProcessing = owner.threads.UnderlyingThreadsContainer.StartBulkProcessing())
					{
						foreach (var loadedMsg in owner.loadedMessages.Forward(0, int.MaxValue))
						{
							var msg = loadedMsg.Message;
							var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(msg);
							if (!LogJoint.Search.SearchInMessageText(msg, preprocessedSearchOptions, bulkSearchState).HasValue)
								continue;
							owner.stats.SearchResultMessagesCount++;
							currentRange.Add(msg.Clone(), false);
							if (owner.stats.SearchResultMessagesCount >= maxHitsCount)
							{
								response.HitsLimitReached = true;
								break;
							}
						}
					}
				}
				response.Hits = owner.stats.SearchResultMessagesCount;

				owner.AcceptStats(LogProviderStatsFlag.SearchResultMessagesCount);

				return response;
			}*/

			private void SetFinalSearchPercentageValue()
			{
				if (!loadingInterrupted && owner.stats.SearchCompletionPercentage != 100)
				{
					owner.stats.SearchCompletionPercentage = 100;
					owner.AcceptStats(LogProviderStatsFlag.SearchCompletionPercentage);
				}
			}

			private void UpdateSearchCompletionPercentage(
				long lastHandledPosition, 
				FileRange.Range fullSearchPositionsRange,
				bool skipMessagesCountCheck)
			{
				if (!skipMessagesCountCheck && (messagesReadSinceCompletionPercentageUpdate % 256) != 0)
				{
					++messagesReadSinceCompletionPercentageUpdate;
				}
				else
				{
					int value;
					if (fullSearchPositionsRange.Length > 0)
						value = (int)Math.Max(0, (lastHandledPosition - fullSearchPositionsRange.Begin) * 100 / fullSearchPositionsRange.Length);
					else
						value = 0;
					if (value != owner.stats.SearchCompletionPercentage)
					{
						owner.stats.SearchCompletionPercentage = value;
						owner.AcceptStats(LogProviderStatsFlag.SearchCompletionPercentage);
					}
					messagesReadSinceCompletionPercentageUpdate = 0;
				}
			}

			private void DoFlush(bool reallocateMessageBuffers = false)
			{
				if (FlushBuffer(reallocateMessageBuffers))
				{
					messagesReadSinceLastFlush = 0;
					lastTimeFlushed = Environment.TickCount;
				}
			}

			private void ProcessLastReadMessageAndFlushIfItsTimeTo(bool reallocateMessageBuffers = false)
			{
				int checkFlushConditionEvery = 2 * 1024;

				bool flushed = false;
				if (ProcessLastReadMessageAndFlush(reallocateMessageBuffers))
				{
					flushed = true;
				}
				else if ((messagesReadSinceLastFlush % checkFlushConditionEvery) == 0
					  && (Environment.TickCount - lastTimeFlushed) > 1000)
				{
					FlushBuffer(reallocateMessageBuffers);
					flushed = true;
				}
				else
				{
					++messagesReadSinceLastFlush;
				}
				if (flushed)
				{
					messagesReadSinceLastFlush = 0;
					lastTimeFlushed = Environment.TickCount;
				}

			}

			IPositionedMessagesReader reader;
			readonly RangeManagingProvider owner;
			MessagesContainers.MessagesRange currentRange;
			MessagesContainers.RangesManagingCollection currentMessagesContainer;
			List<IMessage> readBuffer = new List<IMessage>();
			IMessage lastReadMessage;
			Exception loadError;
			bool breakAlgorithm;
			bool loadingInterrupted;
			int lastTimeFlushed;
			int messagesReadSinceLastFlush;
			int messagesReadSinceCompletionPercentageUpdate;
		};

		void UpdateLoadedTimeStats(IPositionedMessagesReader reader)
		{
			using (tracer.NewFrame)
			{
				MessagesContainers.RangesManagingCollection tmp = messagesCache;

				tracer.Info("Current messages: {0}", tmp);

				int c = tmp.Count;
				if (c != 0)
				{
					DateTime begin = new DateTime();
					DateTime end = new DateTime();
					foreach (IndexedMessage l in tmp.Forward(0, 1))
					{
						tracer.Info("First message: {0}, {1}", l.Message.Time, l.Message.Text);
						begin = l.Message.Time.ToLocalDateTime();
					}
					foreach (IndexedMessage l in tmp.Reverse(c - 1, c - 2))
					{
						tracer.Info("Last message: {0}, {1}", l.Message.Time, l.Message.Text);
						end = l.Message.Time.ToLocalDateTime();
					}
					stats.LoadedTime = DateRange.MakeFromBoundaryValues(begin, end);
				}
				else
				{
					stats.LoadedTime = DateRange.MakeEmpty();
				}
				stats.IsFullyLoaded = tmp.ActiveRange.End >= reader.EndPosition;
				stats.IsShiftableDown = tmp.ActiveRange.End < reader.EndPosition;
				stats.IsShiftableUp = tmp.ActiveRange.Begin > reader.BeginPosition;

				long bytesCount = 0;
				foreach (MessagesContainers.MessagesRange lr in tmp.Ranges)
					bytesCount += reader.PositionRangeToBytes(lr.LoadedRange);
				stats.LoadedBytes = bytesCount;

				tracer.Info("Calculated statistics: LoadedTime={0}, BytesLoaded={1}", stats.LoadedTime, stats.LoadedBytes);

				AcceptStats(LogProviderStatsFlag.LoadedTime | LogProviderStatsFlag.BytesCount);
			}
		}

		protected void InvalidateMessages()
		{
			using (tracer.NewFrame)
			{
				if (IsDisposed)
					return;

				bool fireMessagesChanged = false;

				lock (messagesCacheLock)
				{
					if (messagesCache.Count > 0)
					{
						messagesCache.InvalidateMessages();
						fireMessagesChanged = true;
					}
				}

				stats.LoadedBytes = 0;
				stats.LoadedTime = DateRange.MakeEmpty();
				stats.MessagesCount = 0;
				stats.FirstMessageWithTimeConstraintViolation = null;
				AcceptStats(LogProviderStatsFlag.LoadedTime | LogProviderStatsFlag.BytesCount | 
					LogProviderStatsFlag.LoadedMessagesCount | LogProviderStatsFlag.FirstMessageWithTimeConstraintViolation);
			}
		}

		protected override void InvalidateEverythingThatHasBeenLoaded()
		{
			lock (messagesCacheLock)
			{
				InvalidateMessages();
				base.InvalidateEverythingThatHasBeenLoaded();
			}
		}

		protected virtual void RefreshHook()
		{
		}

		protected abstract IPositionedMessagesReader GetReader();

		readonly object messagesCacheLock = new object(); // todo: consider removing the lock - cache seems to be used only locally
		MessagesContainers.RangesManagingCollection messagesCache = new MessagesContainers.RangesManagingCollection();
	}
}
