using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public interface IPositionedMessagesParser : IDisposable
	{
		MessageBase ReadNext();
	};

	/// <summary>
	/// IPositionedMessagesProvider is a generalization of a text log file.
	/// It represents the stream of data that support random positioning.
	/// Positions are long intergers. The stream has boundaries - BeginPosition, EndPosition.
	/// EndPosition - is a valid position but represents past-the-end position of the stream.
	/// To read messages from the stream one uses a 'reader'. Readers created by CreateReader().
	/// </summary>
	/// <remarks>
	/// IPositionedMessagesProvider introduces 'read-message-from-the-middle' problem.
	/// The problem is that a client can set the position that points somewhere
	/// in the middle of a message. If the client creates a reader then, this reader
	/// can successfully read something that it thinks is a correct message. But it
	/// wouldn't be a correct message because it would be only half of the message 
	/// parsed by a chance. To tackle this problem the client should read at least two
	/// messages one after the other. That guarantees that the second message has 
	/// correct beginning position. Generally the client cannot be sure that 
	/// the first message starts correctly.
	/// </remarks>
	public interface IPositionedMessagesProvider : IDisposable
	{
		/// <summary>
		/// Returns the minimum allowed position for this stream
		/// </summary>
		long BeginPosition { get; }
		/// <summary>
		/// Returns past-the-end position of the stream. That means that it
		/// is a valid position but there cannot be a message at this position.
		/// </summary>
		long EndPosition { get; }
		/// <summary>
		/// Updates the stream boundaries detecting them from actual media (file for instance).
		/// </summary>
		/// <param name="incrementalMode">If <value>true</value> allows the provider to optimize 
		/// the operation with assumption that the boundaries have been calculated already and
		/// need to be recalculated only if the actual media has changed.</param>
		/// <returns>Returns <value>true</value> if the boundaries have actually changed. Return value can be used for optimization.</returns>
		bool UpdateAvailableBounds(bool incrementalMode);

		/// <summary>
		/// Returns position's distance that the provider recommends 
		/// as the radius of the range that client may read from this provider.
		/// This property defines the recommended limit of messages
		/// that could be read and kept in the memory at a time.
		/// </summary>
		long ActiveRangeRadius { get; }

		long PositionRangeToBytes(FileRange.Range range);

		/// <summary>
		/// Creates an object that reads messages from provider's media.
		/// </summary>
		/// <param name="startPosition">
		/// Parser starts from position defined by <paramref name="startPosition"/>.
		/// The first read message may have position bigger than <paramref name="startPosition"/>.
		/// </param>
		/// <param name="range">Defines the range of positions that the parser should stay in. 
		/// If <value>null</value> is passed then the parser is limited by provider's BeginPosition/EndPosition</param>
		/// <param name="isMainStreamReader"></param>
		/// <returns>Returns parser object. It must be disposed when is not needed.</returns>
		/// <remarks>
		/// <paramref name="startPosition"/> doesn't have to point to the beginning of a message.
		/// It it provider's responsibility to guarantee that the correct nearest message is read.
		/// </remarks>
		IPositionedMessagesParser CreateParser(long startPosition, FileRange.Range? range, bool isMainStreamReader);
	};

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
			Monitor.Enter(messagesLock);
		}

		public override void UnlockMessages()
		{
			CheckDisposed();
			Monitor.Exit(messagesLock);
		}

		#endregion

		protected override Algorithm CreateAlgorithm()
		{
			return new RangeManagingAlgorithm(this);
		}
		protected abstract IPositionedMessagesProvider GetProvider();

		protected class RangeManagingAlgorithm : AsyncLogReader.Algorithm
		{
			public RangeManagingAlgorithm(RangeManagingReader owner)
				: base(owner)
			{
				this.owner = owner;
				this.provider = owner.GetProvider();
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				return provider.UpdateAvailableBounds(incrementalMode);
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
							fillRanges = UpdateAvailableTime(true) && Cut(owner.stats.AvailableTime.Value);
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
				ret.Position = PositionedMessagesUtils.LocateDateBound(provider, cmd.Date.Value, cmd.Bound);
				tracer.Info("Position to return: {0}", ret.Position);

				if (ret.Position == provider.EndPosition)
				{
					ret.IsEndPosition = true;
					tracer.Info("It is END position");
				}
				else if (ret.Position == provider.BeginPosition - 1)
				{
					ret.IsBeforeBeginPosition = true;
					tracer.Info("It is BEGIN-1 position");
				}
				else
				{
					ret.Date = PositionedMessagesUtils.ReadNearestDate(provider, ret.Position);
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

			void FlushBuffer()
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
							currentRange.Add(m);
							messagesChanged = true;
						}
						catch (MessagesContainers.TimeConstraintViolationException)
						{
						}
					}
					if (messagesChanged)
					{
						owner.stats.MessagesCount = owner.messages.Count;
					}
				}
				if (messagesChanged)
				{
					owner.AcceptStats(StatsFlag.MessagesCount);
					owner.host.OnMessagesChanged();
				}

				readBuffer.Clear();
			}

			void HandleFlags()
			{
				if (lastReadMessage != null)
				{
					readBuffer.Add(lastReadMessage);
					lastReadMessage = null;

					if (readBuffer.Count >= 1024)
					{
						FlushBuffer();
					}
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

					long pos1;
					if (d1 > owner.stats.LoadedTime.Begin)
					{
						pos1 = PositionedMessagesUtils.LocateDateBound(provider, d1, PositionedMessagesUtils.ValueBound.Lower);
					}
					else
					{
						pos1 = owner.messages.ActiveRange.Begin;
					}

					long pos2;
					if (d2 < owner.stats.LoadedTime.End)
					{
						pos2 = PositionedMessagesUtils.LocateDateBound(provider, d2, PositionedMessagesUtils.ValueBound.Lower);
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
					long radius = provider.ActiveRangeRadius;

					switch (align & (NavigateFlag.AlignMask | NavigateFlag.OriginMask))
					{
						case NavigateFlag.OriginDate | NavigateFlag.AlignCenter:
							if (!dateIsInAvailableRange)
								break;
							long lowerPos = PositionedMessagesUtils.LocateDateBound(provider, d.Value, PositionedMessagesUtils.ValueBound.Lower);
							long upperPos = PositionedMessagesUtils.LocateDateBound(provider, d.Value, PositionedMessagesUtils.ValueBound.Upper);
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
									bpos = PositionedMessagesUtils.FindNextMessagePosition(provider, r.Begin);
								}
							}
							if (bpos == null)
							{
								if (!dateIsInAvailableRange)
									break;
								bpos = PositionedMessagesUtils.LocateDateBound(provider, d.Value, PositionedMessagesUtils.ValueBound.Lower);
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
									tpos = PositionedMessagesUtils.FindPrevMessagePosition(provider, r.End);
								}
							}
							if (tpos == null)
							{
								if (!dateIsInAvailableRange)
									break;
								tpos = PositionedMessagesUtils.LocateDateBound(provider, d.Value, PositionedMessagesUtils.ValueBound.Lower);
							}
							ConstrainedNavigate(tpos.Value, tpos.Value + radius * 2);
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
						SetActiveRange(0, 0);
					}
				}
			}

			void LoadHead(DateTime endDate)
			{
				using (tracer.NewFrame)
				{
					long pos1 = provider.BeginPosition;

					long pos2 = Math.Min(
						PositionedMessagesUtils.LocateDateBound(provider, endDate, PositionedMessagesUtils.ValueBound.Lower),
						pos1 + provider.ActiveRangeRadius
					);

					SetActiveRange(pos1, pos2);
				}
			}

			void LoadTail(DateTime beginDate)
			{
				using (tracer.NewFrame)
				{
					long endPos = provider.EndPosition;

					long beginPos = Math.Max(PositionedMessagesUtils.LocateDateBound(provider, beginDate, PositionedMessagesUtils.ValueBound.Lower), 
						endPos - provider.ActiveRangeRadius);

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

								ResetFlags(null);

								// Start reading elements
								using (IPositionedMessagesParser parser = provider.CreateParser(
										currentRange.GetPositionToStartReadingFrom(), currentRange.DesirableRange, true))
								{
									// We need to check breakAlgorithm flag here, because
									// we may reach the end of the stream right after XmlTextReader created.
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

										if (owner.CommandHasToBeInterruped())
										{
											loadingInterruped = true;
											break;
										}

									}
								}

								FlushBuffer();
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
								}
								currentRange = null;
							}

							if (loadingInterruped)
							{
								tracer.Info("Loading interrupted. Stopping to read ranges.");
								break;
							}
						}

						lock (owner.messagesLock)
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

			void ReadNextMessage(IPositionedMessagesParser parser)
			{
				try
				{
					lastReadMessage = parser.ReadNext();
					if (lastReadMessage == null)
					{
						breakAlgorithm = true;
					}
					else
					{
						ProcessSpecialMessages(lastReadMessage);
					}
				}
				catch (Exception e)
				{
					loadError = e;
					breakAlgorithm = true;
				}
			}

			void ProcessSpecialMessages(MessageBase msg)
			{
				if ((msg.Flags & MessageBase.MessageFlag.TypeMask) == MessageBase.MessageFlag.Content)
				{
					if (!msg.Thread.IsInitialized)
					{
						string txt = msg.Text;
						if (txt.StartsWith(Listener.ThreadInfoPrefix, StringComparison.OrdinalIgnoreCase))
						{
							msg.Thread.Init(txt.Substring(Listener.ThreadInfoPrefix.Length));
						}
					}
				}
			}

			IPositionedMessagesProvider provider;
			readonly RangeManagingReader owner;
			MessagesContainers.MessagesRange currentRange;
			List<MessageBase> readBuffer = new List<MessageBase>();
			MessageBase lastReadMessage;
			Exception loadError;
			bool breakAlgorithm;
			bool loadingInterruped;
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
					bytesCount += provider.PositionRangeToBytes(lr.LoadedRange);
				stats.LoadedBytes = bytesCount;

				tracer.Info("Calculated statistics: LoadedTime={0}, BytesLoaded={1}", stats.LoadedTime, stats.LoadedBytes);

				AcceptStats(StatsFlag.LoadedTime | StatsFlag.BytesCount);
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
