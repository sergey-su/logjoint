using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public class TimeGapsDetector : ITimeGapsDetector
	{
		public TimeGapsDetector(LJTraceSource tracer, IInvokeSynchronization modelThreadInvoke, ITimeGapsSource source)
		{
			this.trace = new LJTraceSource("GapsDetector", tracer.Prefix + ".gaps");
			using (trace.NewFrame)
			{
				this.syncInvoke = modelThreadInvoke;
				this.source = source;

				trace.Info("starting worker thread");
				thread = Task.Run((Func<Task>)ThreadProc);
			}
		}

		async Task ITimeGapsDetector.Dispose()
		{
			using (trace.NewFrame)
			{
				trace.Info("setting stop event");
				stopEvt.Set(0);
				trace.Info("waiting for the thread to complete");
				await thread;
				trace.Info("working thread finished");
			}
		}

		public event EventHandler OnTimeGapsChanged;

		bool ITimeGapsDetector.IsWorking
		{
			get { return isWorking; }
		}

		void ITimeGapsDetector.Update(DateRange r)
		{
			using (trace.NewFrame)
			{
				trace.Info("time range passed: {0}", r);

				bool invalidate = false;

				lock (sync)
				{
					TimeSpan threshold = TimeSpan.FromMilliseconds(timeLineRange.Length.TotalMilliseconds / 10.0);
					if (Abs(timeLineRange.Begin - r.Begin) + Abs(timeLineRange.End - r.End) > threshold)
					{
						this.timeLineRange = r;
						invalidate = true;
					}
				}

				if (invalidate)
				{
					trace.Info("setting invalidation event");
					invalidatedEvt.Set(0);
				}
			}
		}

		ITimeGaps ITimeGapsDetector.Gaps
		{
			get 
			{
				lock (sync)
				{
					return gaps ?? emptyGaps;
				}
			}
		}

		TimeSpan Abs(TimeSpan ts)
		{
			if (ts.Ticks < 0)
				return ts.Negate();
			return ts;
		}

		enum ResultCode
		{
			None,
			Ok,
			Stop,
			Invalidate,
			Timeout,
			UserEvent,
		};

		static bool IsStopOrInvalidate(ResultCode r)
		{
			return r == ResultCode.Stop || r == ResultCode.Invalidate;
		}

		async Task<ResultCode> WaitEvents(int timeout, Task userEvent)
		{
			var evts = new List<Task>();
			evts.Add(stopEvt.Wait());
			evts.Add(invalidatedEvt.Wait());
			evts.Add(Task.Delay(timeout));
			if (userEvent != null)
				evts.Add(userEvent);
			var evt = await Task.WhenAny(evts);
			if (evt == evts[0])
			{
				trace.Info("stop event was set");
				return ResultCode.Stop;
			}
			if (evt == evts[1])
			{
				trace.Info("invalidation event was set. Throwing InvalidateException");
				return ResultCode.Invalidate;
			}
			if (evt == evts[2])
			{
				return ResultCode.Timeout;
			}
			if (evt == userEvent)
			{
				return ResultCode.UserEvent;
			}
			return ResultCode.None;
		}

		async Task ThreadProc()
		{
			using (trace.NewFrame)
			{
				int errCount = 0;
				bool refresh = false;
				bool waitBeforeRefresh = false;
				for (; ; )
				{
					try
					{
						ResultCode triggeredEventId = ResultCode.None;
						if (refresh)
						{
							refresh = false;

							if (waitBeforeRefresh)
							{
								trace.Info("waiting before refresh");
								waitBeforeRefresh = false;
								triggeredEventId = await WaitEvents(1000, null);
							}

							if (!IsStopOrInvalidate(triggeredEventId))
							{
								trace.Info("refreshing the gaps");
								this.isWorking = true;
								try
								{
									triggeredEventId = await Refresh();
								}
								finally
								{
									this.isWorking = false;
								}

								if (errCount != 0)
								{
									trace.Info("resetting error counter (value was {0})", errCount);
									errCount = 0;
								}
							}
						}
						else
						{
							trace.Info("sleeping and waiting for events");
							triggeredEventId = await WaitEvents(Timeout.Infinite, null);
						}

						if (triggeredEventId == ResultCode.Stop)
						{
							trace.Info("Stop event was set. Exiting the thread.");
							break;
						}
						else if (triggeredEventId == ResultCode.Invalidate)
						{
							trace.Info("'Invalidate' event was set. Refreshing the gaps.");
							refresh = true;
						}
					}
					catch (Exception e)
					{
						if (errCount == 5)
						{
							trace.Error(e, "Continuous error detected. Giving up.");
							break;
						}
						trace.Error(e, "Error occured (errCount={0}). Invalidating what has been done and refreshing the gaps.", errCount);
						waitBeforeRefresh = true;
						refresh = true;
						errCount++;
					}
				}
			}
		}

		/// <summary>
		/// Saves the state of one gaps detection transaction
		/// </summary>
		class Helper
		{
			readonly TimeGapsDetector owner;
			readonly IInvokeSynchronization invoke;
			readonly LJTraceSource trace;
			readonly ITimeGapsSource source; // always called in model thread

			long currentPosition = long.MinValue;
			MessageTimestamp currentDate = MessageTimestamp.MinValue;

			public Helper(TimeGapsDetector owner)
			{
				this.owner = owner;
				this.invoke = owner.syncInvoke;
				this.trace = new LJTraceSource("GapsDetector", 
					string.Format("{0}.h{1}", owner.trace.Prefix, ++owner.lastHelperId));
				this.source = owner.source;
			}

			public async Task<ResultCode> MoveToDateBound(DateTime d, bool reversedMode)
			{
				using (trace.NewFrame)
				{
					trace.Info("Moving to the date {0} than '{1}' by sending 'get {2} bound' request to all readers",
						reversedMode ? "less (or eq)" : "greater (or eq)", d, reversedMode ? "lower (rev)" : "lower");

					ResultCode resultCode;

					if (reversedMode)
						currentDate = MessageTimestamp.MinValue;
					else
						currentDate = MessageTimestamp.MaxValue;

					Task<DateBoundPositionResponseData> getBoundsTask = null;

					for (int iteration = 0; ; ++iteration)
					{
						trace.Info("it's iteration {0} of trying to send the 'get date bound' request to reader", iteration);
						var modelThreadCall = invoke.Invoke(() =>
						{
							// This code must be executing in the model thread
							using (trace.NewFrame)
							{
								if (source.IsDisposed)
								{
									trace.Warning("reader is disposed");
									// This TimeGapsDetector is probably disposed too or will be soon.
									// Returning null will make the main algorithm wait.
									// During waiting it'll detect stop condition.
									return null;
								}
								trace.Info("the reader is idling. Getting date bound.");
								return source.GetDateBoundPosition(d, 
									reversedMode ?  ListUtils.ValueBound.LowerReversed : ListUtils.ValueBound.Lower, 
									CancellationToken.None
								); // todo: cancellation
							}
						});

						trace.Info("waiting the completion of 'get date bound' request scheduler");
						if (IsStopOrInvalidate(resultCode = await owner.WaitEvents(Timeout.Infinite, modelThreadCall)))
							return resultCode;

						getBoundsTask = await modelThreadCall;

						if (getBoundsTask != null)
						{
							trace.Info("the 'get date bound' request was successfully sent to reader.");
							break;
						}

						trace.Info("reader is not handled. Waiting...");

						if (IsStopOrInvalidate(resultCode = await owner.WaitEvents(1000, null)))
							return resultCode;
					}

					trace.Info("waiting for the response from the reader");
					if (IsStopOrInvalidate(resultCode = await owner.WaitEvents(30000, getBoundsTask)))
						return resultCode;
					if (resultCode != ResultCode.UserEvent)
					{
						trace.Warning("reader didn't respond. Giving up by invalidating current progress.");
						return ResultCode.Invalidate;
					}

					bool ret = HandleResponse(await getBoundsTask, reversedMode);

					trace.Info("returning {0}", ret);

					return ret ? ResultCode.Ok : ResultCode.None;
				}
			}

			public MessageTimestamp CurrentDate
			{
				get { return currentDate; }
			}

			bool HandleResponse(DateBoundPositionResponseData res, bool reversedMode)
			{
				using (trace.NewFrame)
				{
					Predicate<MessageTimestamp> shouldAdvanceDate = d =>
						reversedMode ? d > currentDate : d < currentDate;

					bool readerAdvanced = false;

					trace.Info("reader returned ({0}, {1})", res.Position, res.Date);

					trace.Info("reader's current position: {0}", currentPosition);

					bool advancePosition = true;
					if (reversedMode)
					{
						if (res.IsBeforeBeginPosition)
						{
							trace.Info("it's invalid position (before begin)");
							advancePosition = false;
						}
					}
					else
					{
						if (res.IsEndPosition)
						{
							trace.Info("it's invalid position (end)");
							advancePosition = false;
						}
					}
					if (advancePosition && res.Position > currentPosition)
					{
						trace.Info("reader has advanced its position: {0}", res.Position);
						readerAdvanced = true;
						currentPosition = res.Position;
					}

					bool advanceDate;
					if (!res.Date.HasValue)
						advanceDate = false;
					else 
						advanceDate = shouldAdvanceDate(res.Date.Value);

					if (advanceDate)
					{
						trace.Info("reader might need to advance the current date from {0} to {1}. Getting writer lock to make final decision...", currentDate, res.Date.Value);

						if (shouldAdvanceDate(res.Date.Value))
						{
							trace.Info("reader is really advancing the current date from {0} to {1}", currentDate, res.Date.Value);
							currentDate = res.Date.Value;
						}
						else
						{
							trace.Info("false alarm: reader is not advancing the current date because it has been already advanced to {0} by some other reader", currentDate);
						}
					}

					return readerAdvanced;
				}
			}
		};

		async Task SetNewGaps(TimeGapsImpl gaps)
		{
			lock (sync)
			{
				this.gaps = gaps;
			}
			trace.Info("posting OnTimeGapsChanged event");
			await syncInvoke.Invoke(() => OnTimeGapsChanged(this, EventArgs.Empty));
		}

		class TooManyGapsException : Exception
		{
		};

		async Task<ResultCode> FindGaps(DateRange range, TimeSpan threshold, int? maxGapsCount, List<TimeGap> ret)
		{
			using (trace.NewFrame)
			{
				trace.Info("threshold={0}", threshold.ToString());

				if (threshold.Ticks == 0)
				{
					trace.Warning("threshold is empty");
					return ResultCode.None;
				}

				ResultCode resultCode = ResultCode.None;

				Helper helper = new Helper(this);

				// Below is the actual algorithm of finding the gaps:
				// - we start from the begin of the range (d = range.Begin). 
				//   On the first iteration we find the positions of the messages 
				//   that have the date less than d. 
				// - on the next iterations we are finding out if there are messages
				//   with the date less than (d + threshold) with position different from
				//   the current positions. If yes, then there is no gap on 
				//   interval (d, d + threshold).
				// - If no messages are found on the interval then we encountered with 
				//   a time gap. The end of the gap is located by searching for the
				//   first message that is greated than d.
				TimeSpan cumulativeGapsLen = new TimeSpan();
				for (DateTime d = range.Begin; d < range.End; )
				{
					trace.Info("moving to the lower bound of {0}", d);

					if (IsStopOrInvalidate(resultCode = await helper.MoveToDateBound(d, reversedMode: true)))
						return resultCode;
					if (resultCode == ResultCode.Ok)
					{
						trace.Info("moved successfully. The lower bound is {0}.", helper.CurrentDate);
						d = helper.CurrentDate.Advance(threshold).ToLocalDateTime();
					}
					else
					{
						var gapBegin = helper.CurrentDate.ToLocalDateTime();
						// A tick is needed here becuase CurrentDate is a date of an existing message.  
						// The gap begins right after this date. This tick matters when 
						// we are comparing gap's date range with a date range of messages. 
						// Do not forget: date ranges use the idea that DateRange.End doesn't belong 
						// to the range.
						gapBegin = gapBegin.AddTicks(1);
						trace.Info("no readers advanced. It's time gap starting at {0}", gapBegin);

						trace.Info("moving to the date greater than {0}", d);
						if (IsStopOrInvalidate(resultCode = await helper.MoveToDateBound(d, reversedMode: false)))
							return resultCode;

						DateTime gapEnd = helper.CurrentDate.ToLocalDateTime();
						trace.Info("the end of the gap: {0}", gapEnd);

						if (MessageTimestamp.Compare(helper.CurrentDate, MessageTimestamp.MaxValue) != 0)
							d = helper.CurrentDate.Advance(threshold).ToLocalDateTime();
						else
							d = DateTime.MaxValue;

						TimeGap gap = new TimeGap(new DateRange(gapBegin, gapEnd), cumulativeGapsLen);
						trace.Info("creating new gap {0}", gap);

						ret.Add(gap);

						if (maxGapsCount.HasValue && ret.Count > maxGapsCount.Value)
						{
							throw new TooManyGapsException();
						}

						cumulativeGapsLen = gap.CumulativeLengthInclusive;
					}
				}

				trace.Info("returning {0} gaps", ret.Count);

				return ResultCode.None;
			}
		}

		async Task<ResultCode> Refresh()
		{
			using (trace.NewFrame)
			{
				DateRange range;
				lock (sync)
				{
					range = timeLineRange;
				}
				trace.Info("timeline dates range: {0}", range.ToString());

				ResultCode resultCode = await WaitEvents(0, null);
				if (IsStopOrInvalidate(resultCode))
					return resultCode;

				TimeSpan threshold = TimeSpan.FromMilliseconds(range.Length.TotalMilliseconds / 20);
				var ret = new List<TimeGap>();
				if (IsStopOrInvalidate(resultCode = await FindGaps(range, threshold, null, ret)))
					return resultCode;

				await SetNewGaps(new TimeGapsImpl(ret, range, threshold));

				return ResultCode.None;
			}
		}




		readonly LJTraceSource trace;
		readonly IInvokeSynchronization syncInvoke;
		readonly ITimeGapsSource source;
		readonly Task thread;
		readonly AwaitableVariable<int> stopEvt = new AwaitableVariable<int>(isAutoReset: false);
		readonly AwaitableVariable<int> invalidatedEvt = new AwaitableVariable<int>(isAutoReset: true);
		readonly object sync = new object();

		#region accessed from model thread and TimeGaps worker thread. Access synced by sync.
		DateRange timeLineRange;
		volatile bool isWorking;
		#endregion

		class TimeGapsImpl : ITimeGaps
		{
			public static readonly List<TimeGap> Empty = new List<TimeGap>();

			List<TimeGap> items;
			DateRange range;
			TimeSpan threshold;
			TimeSpan length;

			public TimeGapsImpl(): this(Empty, new DateRange(), new TimeSpan())
			{
			}

			public TimeGapsImpl(List<TimeGap> list, DateRange range, TimeSpan threshold)
			{
				this.items = list;
				this.range = range;
				this.threshold = threshold;
				foreach (TimeGap g in list)
				{
					length += g.Range.Length;
				}
			}

			public DateRange Range { get { return range; } }
			public TimeSpan Threshold { get { return threshold; } }

			public TimeSpan Length
			{
				get { return length; }
			}

			public int BinarySearch(int begin, int end, Predicate<TimeGap> lessThanValueBeingSearched)
			{
				return ListUtils.BinarySearch(items, begin, end, lessThanValueBeingSearched);
			}

			public TimeGap this[int idx] { get { return items[idx]; } }

			public int Count
			{
				get { return items.Count; }
			}

			public IEnumerator<TimeGap> GetEnumerator()
			{
				return items.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return items.GetEnumerator();
			}

		};

		TimeGapsImpl gaps;
		static readonly TimeGapsImpl emptyGaps = new TimeGapsImpl();
		int lastHelperId;
	}

	public class LogSourceGapsSource : ITimeGapsSource
	{
		readonly ILogSource source;

		public LogSourceGapsSource(ILogSource source)
		{
			this.source = source;
		}

		bool ITimeGapsSource.IsDisposed
		{
			get { return source.IsDisposed; }
		}

		Task<DateBoundPositionResponseData> ITimeGapsSource.GetDateBoundPosition(
			DateTime d, 
			ListUtils.ValueBound bound, 
			CancellationToken cancellation
		)
		{
			return source.Provider.GetDateBoundPosition(d, bound, true, LogProviderCommandPriority.BackgroundActivity, cancellation);
		}
	};
}
