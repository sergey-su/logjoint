using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PositionedMessage = LogJoint.MessagesContainers.PositionedMessage;

namespace LogJoint
{
	abstract class RangeManagingReader : AsyncLogReader
	{
		public RangeManagingReader(ILogReaderHost host, ILogReaderFactory factory)
			:
			base(host, factory)
		{ }

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
			Monitor.Enter(messagesSync);
		}

		public override void UnlockMessages()
		{
			CheckDisposed();
			Monitor.Exit(messagesSync);
		}

		#endregion

		protected abstract IPositionedMessagesProvider CreateProvider();

		protected interface IPositionedMessagesReader : IDisposable
		{
			MessageBase ReadNext();
			long GetPositionOfNextMessage();
			long GetPositionBeforeNextMessage();
		};

		protected interface IPositionedMessagesProvider : IDisposable
		{
			long BeginPosition { get; }
			long EndPosition { get; }
			long Position { get; set; }
			long ActiveRangeRadius { get; }
			void LocateDateLowerBound(DateTime d);
			void LocateDateUpperBound(DateTime d);
			IPositionedMessagesReader CreateReader(FileRange.Range? range, bool isMainStreamReader);
			bool UpdateAvailableBounds(bool incrementalMode);
		};

		protected override Algorithm CreateAlgorithm()
		{
			return new RangeManagingAlgorithm(this);
		}

		protected class RangeManagingAlgorithm : AsyncLogReader.Algorithm
		{
			public RangeManagingAlgorithm(RangeManagingReader owner)
				: base(owner)
			{
				this.owner = owner;
				this.provider = owner.CreateProvider();
			}

			public override void Dispose()
			{
				this.provider.Dispose();
				base.Dispose();
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				return provider.UpdateAvailableBounds(incrementalMode);
			}

			protected override void ProcessCommand(Command cmd)
			{
				lock (owner.messagesSync)
				{
					switch (cmd.Type)
					{
						case Command.CommandType.NavigateTo:
							NavigateTo(cmd.Date, cmd.Align);
							break;
						case Command.CommandType.Cut:
							if (!Cut(cmd.Date.Value, cmd.Date2))
								return;
							break;
						case Command.CommandType.LoadHead:
							LoadHead(cmd.Date.Value);
							break;
						case Command.CommandType.LoadTail:
							LoadTail(cmd.Date.Value);
							break;
						case Command.CommandType.UpdateAvailableTime:
							if (!UpdateAvailableTime(true))
								return;
							if (!Cut(owner.stats.AvailableTime.Value))
								return;
							break;
					}
				}

				FillRanges();
			}

			void ResetFlags(IPositionedMessagesReader parser)
			{
				breakAlgorithm = false;
				loadingInterruped = false;
				lastReadMessage = null;
				currentPositionsRangeBegin = currentRange.Range.End;
				if (parser != null)
				{
					currentPositionsRangeBegin = Math.Max(parser.GetPositionBeforeNextMessage(), currentPositionsRangeBegin);
				}
			}

			void HandleFlags()
			{
				if (lastReadMessage != null)
				{
					lock (owner.messagesSync)
					{
						currentRange.Add(lastReadMessage, currentPositionsRangeBegin, provider.Position);
						owner.stats.MessagesCount = owner.messages.Count;
					}
					lastReadMessage = null;
					owner.AcceptStats(StatsFlag.MessagesCount);
					owner.host.OnMessagesChanged();
				}

				if (loadError != null)
				{
					owner.stats.Error = loadError;
					owner.stats.State = ReaderState.LoadError;
					owner.AcceptStats(StatsFlag.State | StatsFlag.Error);
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

					PositionedMessage pos1;
					if (d1 > owner.stats.LoadedTime.Begin)
					{
						provider.LocateDateLowerBound(d1);
						pos1 = SeekAndReadElement(provider, provider.Position);
					}
					else
					{
						pos1 = new PositionedMessage(owner.messages.ActiveRange.Begin, null);
					}

					PositionedMessage pos2;
					if (d2 < owner.stats.LoadedTime.End)
					{
						provider.LocateDateLowerBound(d2);
						pos2 = SeekAndReadElement(provider, provider.Position);
					}
					else
					{
						pos2 = new PositionedMessage(owner.messages.ActiveRange.End, null);
					}

					return SetActiveRange(pos1, pos2);
				}
			}

			bool SetActiveRange(PositionedMessage pos1, PositionedMessage pos2)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("Messages before changing the active range: {0}", owner.messages);

					if (owner.messages.SetActiveRange(pos1, pos2))
					{
						tracer.Info("Messages changed. New messages: {0}", owner.messages);

						owner.stats.MessagesCount = owner.messages.Count;
						owner.AcceptStats(StatsFlag.MessagesCount);

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
					if (p1 < provider.BeginPosition)
					{
						p2 = Math.Min(provider.EndPosition, p2 + provider.BeginPosition - p1);
						p1 = provider.BeginPosition;
					}
					if (p2 >= provider.EndPosition)
					{
						p1 = Math.Max(provider.BeginPosition, p1 - p2 + provider.EndPosition);
						p2 = provider.EndPosition;
					}

					SetActiveRange(
						SeekAndReadElement(provider, p1),
						SeekAndReadElement(provider, p2)
					);
				}
			}

			static long FindNextMessagePosition(IPositionedMessagesProvider provider, 
				long initialPos, long posDelta)
			{
				for (long pos = initialPos; ; )
				{
					if (pos <= 0)
						break;
					pos += posDelta; 
					provider.Position = Utils.PutInRange(provider.BeginPosition,
						provider.EndPosition, pos);
					long tmp;
					using (IPositionedMessagesReader parser = provider.CreateReader(null, false))
						tmp = parser.GetPositionOfNextMessage();
					if (tmp != initialPos)
						return tmp;
				}
				return initialPos;
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
					long radius = provider.ActiveRangeRadius;
					switch (align & (NavigateFlag.AlignMask | NavigateFlag.OriginMask))
					{
						case NavigateFlag.OriginDate | NavigateFlag.AlignCenter:
							if (!dateIsInAvailableRange)
								break;
							provider.LocateDateLowerBound(d.Value);
							long lowerPos = provider.Position;
							provider.LocateDateUpperBound(d.Value);
							long upperPos = provider.Position;
							long center = (lowerPos + upperPos) / 2;

							ConstrainedNavigate(
								center - radius, center + radius);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginDate | NavigateFlag.AlignBottom:
							long bpos = -1;
							if ((align & NavigateFlag.ShiftingMode) != 0)
							{
								FileRange.Range r = owner.messages.ActiveRange;
								if (!r.IsEmpty)
								{
									bpos = FindNextMessagePosition(provider, r.Begin, 6);
								}
							}
							if (bpos == -1)
							{
								if (!dateIsInAvailableRange)
									break;
								provider.LocateDateLowerBound(d.Value);
								bpos = provider.Position;
							}
							ConstrainedNavigate(bpos - radius * 2, bpos);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginDate | NavigateFlag.AlignTop:
							long tpos = -1;
							if ((align & NavigateFlag.ShiftingMode) != 0)
							{
								FileRange.Range r = owner.messages.ActiveRange;
								if (!r.IsEmpty)
								{
									tpos = FindNextMessagePosition(provider, r.End, -6);
								}
							}
							if (tpos == -1)
							{
								if (!dateIsInAvailableRange)
									break;
								provider.LocateDateLowerBound(d.Value);
								tpos = provider.Position;
							}
							ConstrainedNavigate(tpos, tpos + radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignTop:
							ConstrainedNavigate(0, radius * 2);
							navigateToNowhere = false;
							break;

						case NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom:
							ConstrainedNavigate(provider.EndPosition - radius * 2, provider.EndPosition);
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
						SetActiveRange(new PositionedMessage(), new PositionedMessage());
					}
				}
			}

			void LoadHead(DateTime endDate)
			{
				using (tracer.NewFrame)
				{
					long startPos = provider.BeginPosition;
					PositionedMessage pos1 = SeekAndReadElement(provider, startPos);

					provider.LocateDateLowerBound(endDate);
					long pos = Math.Min(provider.Position, startPos + provider.ActiveRangeRadius);
					PositionedMessage pos2 = SeekAndReadElement(provider, pos);

					SetActiveRange(pos1, pos2);
				}
			}

			void LoadTail(DateTime beginDate)
			{
				using (tracer.NewFrame)
				{
					long endPos = provider.EndPosition;

					provider.LocateDateLowerBound(beginDate);
					long pos = Math.Max(provider.Position, endPos - provider.ActiveRangeRadius);
					PositionedMessage pos1 = SeekAndReadElement(provider, pos);

					PositionedMessage pos2 = SeekAndReadElement(provider, endPos);

					SetActiveRange(pos1, pos2);
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
							lock (owner.messagesSync)
							{
								currentRange = owner.messages.GetNextRangeToFill();
							}

							if (currentRange == null) // Nothing to fill. Everything that is needed is read.
							{
								break;
							}

							try
							{

								tracer.Info("currentRange={0}", currentRange);

								if (!updateStarted)
								{
									tracer.Info("Starting to update the messages.");

									owner.stats.State = ReaderState.Loading;
									owner.AcceptStats(StatsFlag.State);

									updateStarted = true;
								}

								// Locate the end of the current range. 
								// Note: end of the range points to the beginning of 
								// an element (or to the end of stream).
								provider.Position = currentRange.Range.End;

								ResetFlags(null);

								// Start reading elements
								using (IPositionedMessagesReader parser = provider.CreateReader(currentRange.DesirableRange, true))
								{
									// We need check breakAlgorithm flag here, because
									// We may reach the end of the stream right after XmlTextReader created.
									if (breakAlgorithm)
										break;

									// Iterate through the elements in the stream
									for (; ; )
									{
										// Reset the state of the reader
										ResetFlags(parser);

										// Actually read the message
										ReadNextMessage(parser);

										// Handle the flags
										HandleFlags();
										
										if (breakAlgorithm)
											break;

										if (currentRange.StopReadingAllowed
										 && owner.CommandHasToBeInterruped())
										{
											loadingInterruped = true;
											break;
										}

									}
								}
							}
							finally
							{
								lock (owner.messagesSync)
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
								}
								currentRange = null;
							}

							if (loadingInterruped)
							{
								tracer.Info("Loading interrupted. Stopping to read ranges.");
								break;
							}
						}

						lock (owner.messagesSync)
						{
							owner.UpdateLoadedTimeStats(provider);
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

			PositionedMessage SeekAndReadElement(IPositionedMessagesProvider stream, long pos)
			{
				MessageBase ret = null;
				stream.Position = Utils.PutInRange(stream.BeginPosition, stream.EndPosition, pos);
				long savePos;
				using (IPositionedMessagesReader parser = provider.CreateReader(null, false))
				{
					savePos = parser.GetPositionOfNextMessage();
					ret = parser.ReadNext();
				}
				stream.Position = savePos;
				return new PositionedMessage(savePos, ret);
			}

			void ReadNextMessage(IPositionedMessagesReader parser)
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

			readonly RangeManagingReader owner;
			IPositionedMessagesProvider provider;
			MessagesContainers.MessagesRange currentRange;
			MessageBase lastReadMessage;
			Exception loadError;
			bool breakAlgorithm;
			bool loadingInterruped;
			long currentPositionsRangeBegin;
		};

		void UpdateLoadedTimeStats(IPositionedMessagesProvider provider)
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
				stats.IsFullyLoaded = tmp.ActiveRange.Length >= provider.ActiveRangeRadius * 2;
				stats.IsShiftableDown = tmp.ActiveRange.End < provider.EndPosition;
				stats.IsShiftableUp = tmp.ActiveRange.Begin > provider.BeginPosition;

				long bytesCount = 0;
				foreach (MessagesContainers.MessagesRange lr in tmp.Ranges)
					bytesCount += lr.Range.Length;
				stats.LoadedBytes = bytesCount;

				tracer.Info("Calculated statistics: LoadedTime={0}, BytesLoaded={1}", stats.LoadedTime, stats.LoadedBytes);

				AcceptStats(StatsFlag.LoadedTime | StatsFlag.BytesCount);
			}
		}

		protected void InvalidateMessages()
		{
			using (tracer.NewFrame)
			{
				lock (messagesSync)
				{
					messages.InvalidateMessages();
				}
			}
		}

		protected override void InvalidateEverythingThatHasBeenLoaded()
		{
			InvalidateMessages();
			base.InvalidateEverythingThatHasBeenLoaded();
		}

		readonly object messagesSync = new object();
		MessagesContainers.Messsages messages = new MessagesContainers.Messsages();
	}
}
