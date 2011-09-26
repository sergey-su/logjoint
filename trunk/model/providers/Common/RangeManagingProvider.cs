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

		public override IMessagesCollection Messages
		{
			get
			{
				CheckDisposed();
				return messages;
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
							break;

					}
				}

				if (cmd.Type == Command.CommandType.GetDateBound)
				{
					if (retVal == null)
					{
						tracer.Info("Date bound was not found among messages the memory. Looking in the log media");
						retVal = GetDateBoundFromMedia(cmd);
					}
				}

				if (fillRanges)
				{
					FillRanges();
				}

				return retVal;
			}

			DateBoundPositionResponceData GetDateBoundFromMedia(Command cmd)
			{
				DateBoundPositionResponceData ret = new DateBoundPositionResponceData();
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

			void ResetFlags(IPositionedMessagesParser parser)
			{
				breakAlgorithm = false;
				loadingInterruped = false;
				lastReadMessage = null;
			}

			void FlushBuffer(bool allowAsyncFlush)
			{
				if (readBuffer.Count == 0)
					return;

				bool messagesChanged = false;

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
							owner.tracer.Warning("Time contraint violation. Message: %s %s", m.Time.ToString(), m.Text);
						}
					}
					if (messagesChanged)
					{
						owner.stats.MessagesCount = owner.messages.Count;
					}
				}
				if (messagesChanged)
				{
					owner.AcceptStats(LogProviderStatsFlag.MessagesCount);
					owner.host.OnMessagesChanged();
				}
				readBuffer.Clear();
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

			private void ProcessLastReadMessage()
			{
				if (lastReadMessage != null)
				{
					readBuffer.Add(lastReadMessage);
					lastReadMessage = null;

					if (readBuffer.Count >= 1024)
					{
						FlushBuffer(true);
					}
				}
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
						pos1 = owner.messages.ActiveRange.Begin;
					}

					long pos2;
					if (d2 < owner.stats.LoadedTime.End)
					{
						pos2 = PositionedMessagesUtils.LocateDateBound(reader, d2, PositionedMessagesUtils.ValueBound.Lower);
					}
					else
					{
						pos2 = owner.messages.ActiveRange.End;
					}

					return SetActiveRange(pos1, pos2);
				}
			}

			bool SetActiveRange(long pos1, long pos2)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("Messages before changing the active range: {0}", owner.messages);

					pos1 = PositionedMessagesUtils.NormalizeMessagePosition(reader, pos1);
					pos2 = PositionedMessagesUtils.NormalizeMessagePosition(reader, pos2);

					if (owner.messages.SetActiveRange(pos1, pos2))
					{
						tracer.Info("Messages changed. New messages: {0}", owner.messages);

						owner.stats.MessagesCount = owner.messages.Count;
						owner.AcceptStats(LogProviderStatsFlag.MessagesCount);

						owner.host.OnMessagesChanged();
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
								FileRange.Range r = owner.messages.ActiveRange;
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
								FileRange.Range r = owner.messages.ActiveRange;
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
							long loadedRangeBegin = owner.messages.ActiveRange.Begin;
							ConstrainedNavigate(loadedRangeBegin, loadedRangeBegin + radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginLoadedRangeBoundaries | NavigateFlag.AlignBottom:
							long loadedRangeEnd = owner.messages.ActiveRange.End;
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
						// Iterate through the ranges to read
						for (; ; )
						{
							lock (owner.messagesLock)
							{
								currentRange = owner.messages.GetNextRangeToFill();
								if (currentRange == null) // Nothing to fill. Everything that is needed is read.
								{
									break;
								}
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

								ResetFlags(null);

								// Start reading elements
								using (IPositionedMessagesParser parser = reader.CreateParser(new CreateParserParams(
										currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange,
										MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading, 
										MessagesParserDirection.Forward)))
								{
									// We need to check breakAlgorithm flag here, because
									// we may reach the end of the stream right after XmlTextReader created.
									if (breakAlgorithm)
										break;

									// Iterate through the elements in the stream
									for (; ; )
									{
										ResetFlags(parser);

										ReadNextMessage(parser);

										ProcessLastReadMessage();

										ReportLoadErrorIfAny();

										if (breakAlgorithm)
										{
											break;
										}

										++messagesRead;

										if (owner.CommandHasToBeInterruped())
										{
											loadingInterruped = true;
											break;
										}

									}


								}

								FlushBuffer(false);

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
									if (!loadingInterruped)
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
								}
							}

							if (loadingInterruped)
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

			IPositionedMessagesReader reader;
			readonly RangeManagingProvider owner;
			MessagesContainers.MessagesRange currentRange;
			List<MessageBase> readBuffer = new List<MessageBase>();
			MessageBase lastReadMessage;
			Exception loadError;
			bool breakAlgorithm;
			bool loadingInterruped;
		};

		void UpdateLoadedTimeStats(IPositionedMessagesReader reader)
		{
			using (tracer.NewFrame)
			{
				MessagesContainers.Messsages tmp = messages;

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
					messages.InvalidateMessages();
				}
			}
		}

		protected override void InvalidateEverythingThatHasBeenLoaded()
		{
			lock (messagesLock)
			{
				InvalidateMessages();
				base.InvalidateEverythingThatHasBeenLoaded();
			}
		}

		readonly object messagesLock = new object();
		MessagesContainers.Messsages messages = new MessagesContainers.Messsages();
	}
}
