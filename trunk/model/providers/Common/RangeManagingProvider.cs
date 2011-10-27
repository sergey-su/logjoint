using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace LogJoint
{
	public abstract class RangeManagingProvider : AsyncLogProvider
	{
		public RangeManagingProvider(ILogProviderHost host, ILogProviderFactory factory)
			:
			base(host, factory)
		{
		}

		#region ILogReader methods

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

		#endregion

		protected class RangeManagingAlgorithm : AsyncLogProvider.Algorithm
		{
			MessageBase firstMessage;

			public RangeManagingAlgorithm(RangeManagingProvider owner, IPositionedMessagesReader reader)
				: base(owner)
			{
				this.owner = owner;
				this.reader = reader;
			}

			private static DateRange? GetAvailableDateRangeHelper(MessageBase first, MessageBase last)
			{
				if (first == null || last == null)
					return null;
				return DateRange.MakeFromBoundaryValues(first.Time, last.Time);
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				UpdateBoundsStatus status = reader.UpdateAvailableBounds(incrementalMode);

				if (status == UpdateBoundsStatus.NothingUpdated)
				{
					return false;
				}

				if (status == UpdateBoundsStatus.OldMessagesAreInvalid)
				{
					incrementalMode = false;
				}

				// Get new boundary values into temporary variables
				MessageBase newFirst, newLast;
				PositionedMessagesUtils.GetBoundaryMessages(reader, null, out newFirst, out newLast);

				if (firstMessage != null)
				{
					if (newFirst == null || newFirst.Time != firstMessage.Time)
					{
						// The first message we've just read differs from the cached one. 
						// This means that the log was overwritten. Fall to non-incremental mode.
						incrementalMode = false;
					}
				}

				if (!incrementalMode)
				{
					// Reset everything that has been loaded so far
					owner.InvalidateEverythingThatHasBeenLoaded();
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
				owner.AcceptStats(f);

				return true;
			}

			protected override object ProcessCommand(Command cmd)
			{
				bool fillRanges = false;
				bool fillSearchResult = false;
				object retVal = null;

				lock (owner.messagesLock)
				{
					switch (cmd.Type)
					{
						case Command.CommandType.NavigateTo:
							NavigateTo(cmd.Date, cmd.Align);
							fillRanges = true;
							break;
						case Command.CommandType.Cut:
							fillRanges = Cut(cmd.Date.Value, cmd.Date2);
							break;
						case Command.CommandType.LoadHead:
							LoadHead(cmd.Date.Value);
							fillRanges = true;
							break;
						case Command.CommandType.LoadTail:
							LoadTail(cmd.Date.Value);
							fillRanges = true;
							break;
						case Command.CommandType.UpdateAvailableTime:
							fillRanges = UpdateAvailableTime(true) && owner.stats.AvailableTime.HasValue && Cut(owner.stats.AvailableTime.Value);
							break;
						case Command.CommandType.GetDateBound:
							if (owner.stats.LoadedTime.IsInRange(cmd.Date.Value))
							{
								// todo: optimize the command for the case when the message with cmd.Date is loaded in memory
							}
							if (retVal == null)
							{
								tracer.Info("Date bound was not found among messages the memory. Looking in the log media");
								retVal = GetDateBoundFromMedia(cmd);
							}
							break;
						case Command.CommandType.Search:
							owner.InvalidateSearchResults();
							fillSearchResult = true;
							break;
					}
				}

				if (fillRanges)
				{
					FillRanges();
				}
				if (fillSearchResult)
				{
					retVal = FillSearchResult(cmd.SearchParams);
				}

				return retVal;
			}

			DateBoundPositionResponseData GetDateBoundFromMedia(Command cmd)
			{
				DateBoundPositionResponseData ret = new DateBoundPositionResponseData();
				ret.Position = PositionedMessagesUtils.LocateDateBound(reader, cmd.Date.Value, cmd.Bound);
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
					ret.Date = PositionedMessagesUtils.ReadNearestDate(reader, ret.Position);
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

			void FlushBuffer()
			{
				if (readBuffer.Count == 0)
					return;

				bool messagesChanged = false;
				int newMessagesCount = 0;

				lock (owner.messagesLock)
				{
					foreach (MessageBase m in readBuffer)
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
						newMessagesCount = currentMessagesContainer.Count;
					}
				}
				readBuffer.Clear();
				if (messagesChanged)
				{
					if (currentMessagesContainer == owner.loadedMessages)
					{
						owner.stats.MessagesCount = newMessagesCount;
						owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount);
						owner.host.OnLoadedMessagesChanged();
					}
					else if (currentMessagesContainer == owner.searchResult)
					{
						owner.stats.SearchResultMessagesCount = newMessagesCount;
						owner.AcceptStats(LogProviderStatsFlag.SearchResultMessagesCount);
						owner.host.OnSearchResultChanged();
					}
				}
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

			private bool ProcessLastReadMessageAndFlush()
			{
				if (lastReadMessage != null)
				{
					readBuffer.Add(lastReadMessage);
					lastReadMessage = null;

					if (readBuffer.Count >= 1024)
					{
						FlushBuffer();
						return true;
					}
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

					long pos1;
					if (d1 > owner.stats.LoadedTime.Begin)
					{
						pos1 = PositionedMessagesUtils.LocateDateBound(reader, d1, PositionedMessagesUtils.ValueBound.Lower);
					}
					else
					{
						pos1 = owner.loadedMessages.ActiveRange.Begin;
					}

					long pos2;
					if (d2 < owner.stats.LoadedTime.End)
					{
						pos2 = PositionedMessagesUtils.LocateDateBound(reader, d2, PositionedMessagesUtils.ValueBound.Lower);
					}
					else
					{
						pos2 = owner.loadedMessages.ActiveRange.End;
					}

					return SetActiveRange(pos1, pos2);
				}
			}

			bool SetActiveRange(long pos1, long pos2)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("Messages before changing the active range: {0}", owner.loadedMessages);

					pos1 = PositionedMessagesUtils.NormalizeMessagePosition(reader, pos1);
					pos2 = PositionedMessagesUtils.NormalizeMessagePosition(reader, pos2);

					if (owner.loadedMessages.SetActiveRange(pos1, pos2))
					{
						tracer.Info("Messages changed. New messages: {0}", owner.loadedMessages);

						owner.stats.MessagesCount = owner.loadedMessages.Count;
						owner.AcceptStats(LogProviderStatsFlag.LoadedMessagesCount);

						owner.host.OnLoadedMessagesChanged();
						return true;
					}
					else
					{
						tracer.Info("Setting a new active range didn't make any change in messages.");
						return false;
					}
				}
			}

			void ConstrainedNavigate(long p1, long p2)
			{
				using (tracer.NewFrame)
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

					SetActiveRange(
						p1,
						p2
					);
				}
			}

			void NavigateTo(DateTime? d, NavigateFlag align)
			{
				using (tracer.NewFrame)
				{
					bool dateIsInAvailableRange = false;
					if ((align & NavigateFlag.OriginDate) != 0)
					{
						System.Diagnostics.Debug.Assert(d != null);
						dateIsInAvailableRange = owner.stats.AvailableTime.Value.IsInRange(d.Value);
					}

					bool navigateToNowhere = true;
					long radius = reader.ActiveRangeRadius;

					switch (align & (NavigateFlag.AlignMask | NavigateFlag.OriginMask))
					{
						case NavigateFlag.OriginDate | NavigateFlag.AlignCenter:
							if (!dateIsInAvailableRange)
								break;
							long lowerPos = PositionedMessagesUtils.LocateDateBound(reader, d.Value, PositionedMessagesUtils.ValueBound.Lower);
							long upperPos = PositionedMessagesUtils.LocateDateBound(reader, d.Value, PositionedMessagesUtils.ValueBound.Upper);
							long center = (lowerPos + upperPos) / 2;

							ConstrainedNavigate(
								center - radius, center + radius);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginDate | NavigateFlag.AlignBottom:
							long? bpos = null;
							if ((align & NavigateFlag.ShiftingMode) != 0)
							{
								FileRange.Range r = owner.loadedMessages.ActiveRange;
								if (!r.IsEmpty)
								{
									bpos = PositionedMessagesUtils.FindNextMessagePosition(reader, r.Begin);
								}
							}
							if (bpos == null)
							{
								if (!dateIsInAvailableRange)
									break;
								bpos = PositionedMessagesUtils.LocateDateBound(reader, d.Value, PositionedMessagesUtils.ValueBound.Lower);
							}
							ConstrainedNavigate(bpos.Value - radius * 2, bpos.Value);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginDate | NavigateFlag.AlignTop:
							long? tpos = null;
							if ((align & NavigateFlag.ShiftingMode) != 0)
							{
								FileRange.Range r = owner.loadedMessages.ActiveRange;
								if (!r.IsEmpty)
								{
									tpos = PositionedMessagesUtils.FindPrevMessagePosition(reader, r.End);
								}
							}
							if (tpos == null)
							{
								if (!dateIsInAvailableRange)
									break;
								tpos = PositionedMessagesUtils.LocateDateBound(reader, d.Value, PositionedMessagesUtils.ValueBound.Lower);
							}
							ConstrainedNavigate(tpos.Value, tpos.Value + radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignTop:
							ConstrainedNavigate(0, radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom:
							ConstrainedNavigate(reader.EndPosition - radius * 2, reader.EndPosition);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginLoadedRangeBoundaries | NavigateFlag.AlignTop:
							long loadedRangeBegin = owner.loadedMessages.ActiveRange.Begin;
							ConstrainedNavigate(loadedRangeBegin, loadedRangeBegin + radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginLoadedRangeBoundaries | NavigateFlag.AlignBottom:
							long loadedRangeEnd = owner.loadedMessages.ActiveRange.End;
							ConstrainedNavigate(loadedRangeEnd - radius * 2, loadedRangeEnd);
							navigateToNowhere = false;
							break;
					}
					if (navigateToNowhere)
					{
						SetActiveRange(0, 0);
					}
				}
			}

			void LoadHead(DateTime endDate)
			{
				using (tracer.NewFrame)
				{
					long pos1 = reader.BeginPosition;

					long pos2 = Math.Min(
						PositionedMessagesUtils.LocateDateBound(reader, endDate, PositionedMessagesUtils.ValueBound.Lower),
						pos1 + reader.ActiveRangeRadius
					);

					SetActiveRange(pos1, pos2);
				}
			}

			void LoadTail(DateTime beginDate)
			{
				using (tracer.NewFrame)
				{
					long endPos = reader.EndPosition;

					long beginPos = Math.Max(PositionedMessagesUtils.LocateDateBound(reader, beginDate, PositionedMessagesUtils.ValueBound.Lower), 
						endPos - reader.ActiveRangeRadius);

					SetActiveRange(beginPos, endPos);
				}
			}

			void FillRanges()
			{
				using (tracer.NewFrame)
				{
					bool updateStarted = false;
					try
					{
						// Iterate through the ranges
						for (; ; )
						{
							lock (owner.messagesLock)
							{
								currentRange = owner.loadedMessages.GetNextRangeToFill();
								if (currentRange == null) // Nothing to fill
								{
									break;
								}
								currentMessagesContainer = owner.loadedMessages;
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

										if (owner.CommandHasToBeInterruped())
										{
											loadingInterrupted = true;
											break;
										}

									}


								}

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
								lock (owner.messagesLock)
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
							}

							if (loadingInterrupted)
							{
								tracer.Info("Loading interrupted. Stopping to read ranges.");
								break;
							}
						}

						lock (owner.messagesLock)
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
					lastReadMessage = parser.ReadNext();
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

			SearchAllOccurencesResponseData FillSearchResult(SearchAllOccurencesParams p)
			{
				using (tracer.NewFrame)
				{
					SearchAllOccurencesResponseData ret = new SearchAllOccurencesResponseData();
					lock (owner.messagesLock)
					{
						currentRange = owner.searchResult.GetNextRangeToFill();
						if (currentRange == null)
							return ret;
						currentMessagesContainer = owner.searchResult;
						tracer.Info("range={0}", currentRange);
					}
					try
					{
						owner.stats.State = LogProviderState.Searching;
						owner.AcceptStats(LogProviderStatsFlag.State);

						ResetFlags();

						var preprocessedOptions = p.Options.Preprocess();
						var bulkSearchState = new Search.BulkSearchState();

						int lastTimeFlushed = Environment.TickCount;
						int messagesReadSinceLastFlush = 0;

						int messagesReadSinceCompletionPercentageUpdate = 0;
						var positionsRange = currentRange.DesirableRange;

						using (var threadsBulkProcessing = owner.threads.UnderlyingThreadsContainer.StartBulkProcessing())
						using (var parser = reader.CreateParser(new CreateParserParams(
								currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange,
								MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading
								 //| MessagesParserFlag.DisableMultithreading
								 ,
								MessagesParserDirection.Forward)))
						{
							for (; ; )
							{
								ResetFlags();

								ReadNextMessage(parser);

								if (lastReadMessage != null)
								{
									UpdateSearchCompletionPercentage(ref messagesReadSinceCompletionPercentageUpdate, positionsRange);

									var preprocessingResult = threadsBulkProcessing.ProcessMessage(lastReadMessage);

									ApplyFilters(p, preprocessingResult.DisplayFilterContext);

									if (lastReadMessage != null)
									{
										ApplySearchCriteria(preprocessedOptions, bulkSearchState);
									}
									if (lastReadMessage != null)
									{
										RegisterHitAndApplyHitsLimit(ret);
									}
								}

								ProcessLastReadMessageAndFlushIfItsTimeTo(ref lastTimeFlushed, ref messagesReadSinceLastFlush);

								ReportLoadErrorIfAny();

								if (breakAlgorithm)
								{
									break;
								}

								if (owner.CommandHasToBeInterruped())
								{
									loadingInterrupted = true;
									break;
								}
							}
						}
						if (readBuffer.Count > 0)
						{
							lock (owner.messagesLock)
								FlushBuffer();
						}
						if (!loadingInterrupted && owner.stats.SearchCompletionPercentage != 100)
						{
							owner.stats.SearchCompletionPercentage = 100;
							owner.AcceptStats(LogProviderStatsFlag.SearchCompletionPercentage);
						}
						ret.SearchWasInterrupted = loadingInterrupted;
						ret.Failure = loadError;
					}
					finally
					{
						lock (owner.messagesLock)
						{
							currentRange.Complete();
							currentRange.Dispose();
							currentRange = null;
							currentMessagesContainer = null;
						}
					}
					return ret;
				}
			}

			private void RegisterHitAndApplyHitsLimit(SearchAllOccurencesResponseData response)
			{
				if (response.Hits == 50000) // todo: get rid of hardcoded values
				{
					response.HitsLimitReached = true;
					breakAlgorithm = true;
					lastReadMessage = null;
				}
				else
				{
					response.Hits++;
				}
			}

			private void ApplySearchCriteria(Search.PreprocessedOptions preprocessedOptions, Search.BulkSearchState bulkSearchState)
			{
				var match = LogJoint.Search.SearchInMessageText(lastReadMessage, preprocessedOptions, bulkSearchState);
				if (!match.HasValue)
					lastReadMessage = null;
			}

			private void ApplyFilters(SearchAllOccurencesParams p, FilterContext filterContext)
			{
				if (p.Filters != null)
				{
					var action = p.Filters.ProcessNextMessageAndGetItsAction(lastReadMessage, filterContext);
					if (action == FilterAction.Exclude)
						lastReadMessage = null;
				}
			}

			private void UpdateSearchCompletionPercentage(ref int messagesReadSinceCompletionPercentageUpdate, FileRange.Range positionsRange)
			{
				if ((messagesReadSinceCompletionPercentageUpdate % 256) != 0)
				{
					++messagesReadSinceCompletionPercentageUpdate;
				}
				else
				{
					int value;
					if (positionsRange.Length > 0)
						value = (int)Math.Max(0, (lastReadMessage.Position - positionsRange.Begin) * 100 / positionsRange.Length);
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

			private void ProcessLastReadMessageAndFlushIfItsTimeTo(ref int lastTimeFlushed, ref int messagesReadSinceLastFlush)
			{
				int checkFlushConditionEvery = 2 * 1024;

				bool flushed = false;
				if (ProcessLastReadMessageAndFlush())
				{
					flushed = true;
				}
				else if ((messagesReadSinceLastFlush % checkFlushConditionEvery) == 0
					  && (Environment.TickCount - lastTimeFlushed) > 1000)
				{
					FlushBuffer();
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
			MessagesContainers.Messsages currentMessagesContainer;
			List<MessageBase> readBuffer = new List<MessageBase>();
			MessageBase lastReadMessage;
			Exception loadError;
			bool breakAlgorithm;
			bool loadingInterrupted;
		};

		void UpdateLoadedTimeStats(IPositionedMessagesReader reader)
		{
			using (tracer.NewFrame)
			{
				MessagesContainers.Messsages tmp = loadedMessages;

				tracer.Info("Current messages: {0}", tmp);

				int c = tmp.Count;
				if (c != 0)
				{
					DateTime begin = new DateTime();
					DateTime end = new DateTime();
					foreach (IndexedMessage l in tmp.Forward(0, 1))
					{
						tracer.Info("First message: {0}, {1}", l.Message.Time, l.Message.Text);
						begin = l.Message.Time;
					}
					foreach (IndexedMessage l in tmp.Reverse(c - 1, c - 2))
					{
						tracer.Info("Last message: {0}, {1}", l.Message.Time, l.Message.Text);
						end = l.Message.Time;
					}
					stats.LoadedTime = DateRange.MakeFromBoundaryValues(begin, end);
				}
				else
				{
					stats.LoadedTime = DateRange.MakeEmpty();
				}
				stats.IsFullyLoaded = tmp.ActiveRange.Length >= reader.ActiveRangeRadius * 2;
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
				lock (messagesLock)
				{
					loadedMessages.InvalidateMessages();
				}
			}
		}

		protected void InvalidateSearchResults()
		{
			using (tracer.NewFrame)
			{
				if (IsDisposed)
					return;

				int prevMessagesCount = searchResult.Count;
				lock (messagesLock)
				{
					searchResult.InvalidateMessages();
					searchResult.SetActiveRange(GetReader().BeginPosition, GetReader().EndPosition);
				}
				if (prevMessagesCount > 0)
				{
					stats.SearchResultMessagesCount = 0;
					AcceptStats(LogProviderStatsFlag.SearchResultMessagesCount);
					host.OnSearchResultChanged();
				}
				if (stats.SearchCompletionPercentage != 0)
				{
					stats.SearchCompletionPercentage = 0;
					AcceptStats(LogProviderStatsFlag.SearchCompletionPercentage);
				}
			}
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

		protected abstract IPositionedMessagesReader GetReader();

		readonly object messagesLock = new object();
		MessagesContainers.Messsages loadedMessages = new MessagesContainers.Messsages();
		MessagesContainers.Messsages searchResult = new MessagesContainers.Messsages();
	}
}
