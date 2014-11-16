using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace LogJoint
{
	public class TimeGapsDetector : ITimeGapsDetector, IDisposable
	{
		public TimeGapsDetector(ITimeGapsHost host)
		{
			using (host.Tracer.NewFrame)
			{
				this.host = host;
				this.trace = host.Tracer;
				this.syncInvoke = host.Invoker;

				thread = new Thread(ThreadProc);
#if !SILVERLIGHT
				thread.Priority = ThreadPriority.BelowNormal;
#endif
				thread.Name = "TimeGaps working thread";
				trace.Info("Startin working thread");
				thread.Start();
			}
		}

		void IDisposable.Dispose()
		{
			using (trace.NewFrame)
			{
				trace.Info("Setting stop event");
				stopEvt.Set();
				trace.Info("Waiting for the thread to complete");
				thread.Join();
				trace.Info("Working thread finished");
				stopEvt.Close();
			}
		}

		public event EventHandler OnTimeGapsChanged;

		bool ITimeGapsDetector.IsWorking
		{
			get { return isWorking; }
		}

		void ITimeGapsDetector.Invalidate()
		{
			using (trace.NewFrame)
			{
				lock (sync)
				{
					gaps = null;
					this.timeLineRange = new DateRange();
				}
				trace.Info("Setting invalidation event");
				invalidatedEvt.Set();
			}
		}

		void ITimeGapsDetector.Update(DateRange r)
		{
			using (trace.NewFrame)
			{
				trace.Info("Time range passed: {0}", r);

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
					trace.Info("Setting invalidation event");
					invalidatedEvt.Set();
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

		delegate void SimpleDelegate();

		class AbortException: Exception
		{
		};

		class InvalidateException: Exception
		{
		};

		int CheckEvents(int timeout, WaitHandle[] extraEvents)
		{
			WaitHandle[] evts = new WaitHandle[2 + (extraEvents != null ? extraEvents.Length : 0)];
			evts[0] = stopEvt;
			evts[1] = invalidatedEvt;
			if (extraEvents != null)
			{
				Array.Copy(extraEvents, 0, evts, 2, extraEvents.Length);
			}
			int evtIdx = WaitHandle.WaitAny(evts, timeout);
			if (evtIdx == 0)
			{
				trace.Info("Stop event was set. Throwing AbortException");
				throw new AbortException();
			}
			if (evtIdx == 1)
			{
				trace.Info("Invalidation event was set. Throwing InvalidateException");
				throw new InvalidateException();
			}
			return evtIdx;
		}

		void CheckEvents(int timeout)
		{
			CheckEvents(timeout, null);
		}

		void ThreadProc()
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
						if (refresh)
						{
							refresh = false;

							if (waitBeforeRefresh)
							{
								trace.Info("Waiting before refresh");
								waitBeforeRefresh = false;
								CheckEvents(1000);
							}

							trace.Info("Refreshing the gaps");
							this.isWorking = true;
							try
							{
								Refresh();
							}
							finally
							{
								this.isWorking = false;
							}

							if (errCount != 0)
							{
								trace.Info("Resetting error counter (value was {0})", errCount);
								errCount = 0;
							}
						}
						else
						{
							trace.Info("Sleeping and waiting for events");
							CheckEvents(Timeout.Infinite);
						}
					}
					catch (AbortException)
					{
						trace.Info("Stop event was set. Exiting the thread.");
						break;
					}
					catch (InvalidateException)
					{
						trace.Info("'Invalidate' event was set. Refreshing the gaps.");
						refresh = true;
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
		/// Object that is used internally by TimeGaps background thread. This object works in multithreaded environment.
		/// Some of its members are called by TimeGaps background thread. Some - by the main application thread (or saying more
		/// correctly by the thread represented by ITimeGapsHost.Invoker). Some members are called by the threads that
		/// handle log readers. See comments for each member to see in which thread they are called.
		/// </summary>
		class Helper: IDisposable
		{
			class SourceStruct
			{
				public long CurrentPosition = long.MinValue;
				public bool IsHandled = false;
			};

			#region Members that don't need any multithreading syncronization. They are immutable and their classes are thread-safe
			readonly TimeGapsDetector owner;
			readonly IInvokeSynchronization invoke;
			readonly LJTraceSource trace;
			static readonly object[] emptyArgs = new object[] { };
			readonly CompletionHandler completion;
			readonly ReaderWriterLock sync = new ReaderWriterLock();
			#endregion

			#region Members that are syncronized throught 'sync' objects
			readonly ManualResetEvent allReadersReturned = new ManualResetEvent(false);
			readonly Dictionary<ILogProvider, SourceStruct> sources = new Dictionary<ILogProvider, SourceStruct>();
			bool isDisposed;
			MessageTimestamp currentDate = MessageTimestamp.MinValue;
			#endregion

			#region Members that are accessed/changed by atomic or interlocked instructions
			int readersToWait = 0;
			int readersAdvanced = 0;
			bool reversedMode = false;
			#endregion

			/// <summary>
			/// Called by TimeGaps background thread
			/// </summary>
			public Helper(TimeGapsDetector owner)
			{
				this.owner = owner;
				this.invoke = owner.syncInvoke;
				this.trace = owner.trace;
				this.completion = CompletionHandler;
			}

			/// <summary>
			/// Called only by TimeGaps background thread. It needs locking on sync 
			/// because it is the only method that might be called in parallel with
			/// callbacks to other threads. All other public methods are called only
			/// from TimeGaps background thread and they wait for the callbacks to 
			/// return. Dispose() can be called without waiting.
			/// </summary>
			public void Dispose()
			{
				using (trace.NewFrame)
				{
					sync.AcquireWriterLock(Timeout.Infinite);
					try
					{
						isDisposed = true;
						allReadersReturned.Close();
						sources.Clear();
					}
					finally
					{
						sync.ReleaseWriterLock();
					}
				}
			}

			/// <summary>
			/// Called by TimeGaps background thread. Submits a callback to the main thread
			/// and waits for return.
			/// </summary>
			/// <returns>Amount of readers read.</returns>
			public int ReadSources()
			{
				using (trace.NewFrame)
				{
					trace.Info("Getting the list of log sources (sumbitting the request to the main thread)");
					ITimeGapsHost host = owner.host;
					IAsynchronousInvokeResult ar = invoke.BeginInvoke((SimpleDelegate)delegate()
					{
						// This code must be executing in the main thread. 
						using (trace.NewFrame)
						{
							sync.AcquireWriterLock(Timeout.Infinite);
							try
							{
								if (isDisposed)
								{
									trace.Warning("Helper is already disposed. No need to get the list of sources");
									return;
								}
								trace.Info("Getting the list of log sources");
								foreach (ILogSource src in host.Sources)
								{
									sources[src.Provider] = new SourceStruct();
									trace.Info("---> found log source: id={0}, name={1}", src.Provider.GetHashCode(), src.DisplayName);
								}
							}
							finally
							{
								sync.ReleaseWriterLock();
							}
						}
					}, emptyArgs);

					trace.Info("Waiting for the request to complete");
					owner.CheckEvents(Timeout.Infinite, new WaitHandle[] { ar.AsyncWaitHandle });

					invoke.EndInvoke(ar); // throw any exception if any

					trace.Info("Returning {0}", sources.Count);
					return sources.Count;
				}
			}

			/// <summary>
			/// Called by TimeGaps background thread. Submits a callbacks to the main thread and to readers' threads
			/// and waits for return.
			/// </summary>
			public bool MoveToDateBound(DateTime d, bool reversedMode)
			{
				using (trace.NewFrame)
				{
					trace.Info("Moving to the date {0} than '{1}' by sending 'get {2} bound' request to all readers",
						reversedMode ? "less (or eq)" : "greater (or eq)", d, reversedMode ? "lower (rev)" : "lower");

					trace.Info("Resetting the counters");
					readersToWait = sources.Count;
					allReadersReturned.Reset();
					readersAdvanced = 0;
					this.reversedMode = reversedMode;
					if (reversedMode)
						currentDate = MessageTimestamp.MinValue;
					else
						currentDate = MessageTimestamp.MaxValue;

					foreach (SourceStruct src in sources.Values)
					{
						src.IsHandled = false;
					}

					int readersToHandle = sources.Count;
					for (int iteration = 0; ; ++iteration)
					{
						trace.Info("It's iteration {0} of trying to send the request to all readers", iteration);
						IAsynchronousInvokeResult ar = invoke.BeginInvoke((SimpleDelegate)delegate()
						{
							// This code must be executing in the main thread. 
							using (trace.NewFrame)
							{
								sync.AcquireReaderLock(Timeout.Infinite);
								try
								{
									if (isDisposed)
									{
										trace.Warning("Helper object is disposed. Ignoring the call.");
										return;
									}
									trace.Info("Sending the request to all readers");
									foreach (KeyValuePair<ILogProvider, SourceStruct> src in sources)
									{
										trace.Info("---> {0}", src.Key.GetHashCode());
										if (src.Key.IsDisposed)
										{
											trace.Warning("Reader is disposed");
											return;
										}
										if (src.Value.IsHandled)
										{
											trace.Info("Already handled. Continuing.");
											continue;
										}
										if (!src.Key.WaitForAnyState(true, false, 0))
										{
											trace.Info("The reader if busy. Continuing with other readers.");
											continue;
										}
										try
										{
											trace.Info("The reader is idling. Sending the request.");
											if (reversedMode)
												src.Key.GetDateBoundPosition(d, PositionedMessagesUtils.ValueBound.LowerReversed, completion);
											else
												src.Key.GetDateBoundPosition(d, PositionedMessagesUtils.ValueBound.Lower, completion);
										}
										catch (Exception e)
										{
											trace.Error(e, "Failed to send the request");
											continue;
										}

										trace.Info("The request has been sent OK. Marking the reader as handled");
										readersToHandle--;
										src.Value.IsHandled = true;
									}
								}
								finally
								{
									sync.ReleaseReaderLock();
								}
							}
						}, emptyArgs);

						trace.Info("Waiting for the request to complete");
						owner.CheckEvents(Timeout.Infinite, new WaitHandle[] { ar.AsyncWaitHandle });

						invoke.EndInvoke(ar);

						if (readersToHandle == 0)
						{
							trace.Info("The request was successfully sent to all readers.");
							break;
						}

						trace.Info("Some of the readers were not handled. Not handled {0} of {1}. Waiting...", readersToHandle, sources.Count);

						owner.CheckEvents(1000);
					}

					trace.Info("Waiting for the responses from all readers");
					if (owner.CheckEvents(30000, new WaitHandle[] { allReadersReturned }) == WaitHandle.WaitTimeout)
					{
						trace.Warning("Some of the readers didn't respond ({0}). Giving up by throwing InvalidateException.", readersToWait);
						throw new InvalidateException();
					}

					bool ret = readersAdvanced != 0;

					trace.Info("Readers that have advanced their positions: {0}; returning {1}", readersAdvanced, ret);

					return ret;
				}
			}

			public MessageTimestamp CurrentDate
			{
				get { return currentDate; }
			}

			bool ShouldAdvanceDate(MessageTimestamp d)
			{
				if (reversedMode)
					return d > currentDate;
				else
					return d < currentDate;
			}

			/// <summary>
			/// Called by a reader's thread
			/// </summary>
			void CompletionHandler(ILogProvider provider, object result)
			{
				using (trace.NewFrame)
				{
					DateBoundPositionResponseData res = (DateBoundPositionResponseData)result;
					if (res == null)
						return; // todo: better handling

					trace.Info("Reader {0} returned ({1}, {2})", provider.GetHashCode(), res.Position, res.Date);

					// Use reader lock to allow multiple callbacks for mutiple readers to be called in parallel
					sync.AcquireReaderLock(Timeout.Infinite);
					try
					{
						if (isDisposed)
						{
							trace.Warning("The helper object is already disposed. Ignoring this completion call.");
							return;
						}
						SourceStruct src = sources[provider];

						trace.Info("Reader's current position: {0}", src.CurrentPosition);

						bool advancePosition = true;
						if (reversedMode)
						{
							if (res.IsBeforeBeginPosition)
							{
								trace.Info("It's invalid position (before begin)");
								advancePosition = false;
							}
						}
						else
						{
							if (res.IsEndPosition)
							{
								trace.Info("It's invalid position (end)");
								advancePosition = false;
							}
						}
						if (advancePosition && res.Position > src.CurrentPosition)
						{
							trace.Info("Reader has advanced its position: {0}", res.Position);
							Interlocked.Increment(ref readersAdvanced);
							src.CurrentPosition = res.Position;
						}

						bool advanceDate;
						if (!res.Date.HasValue)
							advanceDate = false;
						else 
							advanceDate = ShouldAdvanceDate(res.Date.Value);

						if (advanceDate)
						{
							trace.Info("Reader might need to advance the current date from {0} to {1}. Getting writer lock to make final decision...", currentDate, res.Date.Value);

							// We have to upgrade to writer lock temporarly becuase we can't change currentDate actomically
							LockCookie lc = sync.UpgradeToWriterLock(Timeout.Infinite);
							try
							{
								trace.Info("Grabbed writer lock");

								if (ShouldAdvanceDate(res.Date.Value))
								{
									trace.Info("Reader is really advancing the current date from {0} to {1}", currentDate, res.Date.Value);
									currentDate = res.Date.Value;
								}
								else
								{
									trace.Info("False alarm: reader is not advancing the current date because it has been already advanced to {0} by some other reader", currentDate);
								}
							}
							finally
							{
								sync.DowngradeFromWriterLock(ref lc);
								trace.Info("Writer lock released");
							}
						}

						if (Interlocked.Decrement(ref readersToWait) == 0)
						{
							trace.Info("All readers have returned a value. This was a last completion call. Setting completion event");
							allReadersReturned.Set();
						}
					}
					finally
					{
						sync.ReleaseReaderLock();
					}
				}
			}


		};

		void SetNewGaps(TimeGapsImpl gaps)
		{
			lock (sync)
			{
				this.gaps = gaps;
			}
			trace.Info("Posting OnTimeGapsChanged event");
			syncInvoke.BeginInvoke(OnTimeGapsChanged, new object[] { this, EventArgs.Empty });
		}

		class TooManyGapsException : Exception
		{
		};

		List<TimeGap> FindGaps(DateRange range, TimeSpan threshold, int? maxGapsCount)
		{
			using (trace.NewFrame)
			{
				trace.Info("Threshold={0}", threshold.ToString());

				List<TimeGap> ret = new List<TimeGap>();

				if (threshold.Ticks == 0)
				{
					trace.Warning("Threshold is empty");
					return ret;
				}

				using (Helper helper = new Helper(this))
				{

					if (helper.ReadSources() == 0)
					{
						trace.Info("No log sources found.");
						return ret;
					}

					CheckEvents(0);

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
						trace.Info("Moving to the lower bound of {0}", d);

						if (helper.MoveToDateBound(d, true))
						{
							trace.Info("Moved successfully. The lower bound is {0}.", helper.CurrentDate);
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
							trace.Info("No readers advanced. It's time gap starting at {0}", gapBegin);

							trace.Info("Moving to the date greater than {0}", d);
							helper.MoveToDateBound(d, false);

							DateTime gapEnd = helper.CurrentDate.ToLocalDateTime();
							trace.Info("The end of the gap: {0}", gapEnd);

							d = helper.CurrentDate.Advance(threshold).ToLocalDateTime();

							TimeGap gap = new TimeGap(new DateRange(gapBegin, gapEnd), cumulativeGapsLen);
							trace.Info("Creating new gap {0}", gap);

							ret.Add(gap);

							if (maxGapsCount.HasValue && ret.Count > maxGapsCount.Value)
							{
								throw new TooManyGapsException();
							}

							cumulativeGapsLen = gap.CumulativeLengthInclusive;
						}
					}
				}

				trace.Info("Returning {0} gaps", ret.Count);

				return ret;
			}
		}

		void Refresh()
		{
			using (trace.NewFrame)
			{
				DateRange range;
				lock (sync)
				{
					range = timeLineRange;
				}
				trace.Info("Time line dates range: {0}", range.ToString());

				TimeSpan threshold = TimeSpan.FromMilliseconds(range.Length.TotalMilliseconds / 20);
				List<TimeGap> ret = FindGaps(range, threshold, null);

				SetNewGaps(new TimeGapsImpl(ret, range, threshold));
			}
		}




		readonly ITimeGapsHost host;
		readonly LJTraceSource trace;
		readonly IInvokeSynchronization syncInvoke;
		readonly Thread thread;
		readonly ManualResetEvent stopEvt = new ManualResetEvent(false);
		readonly AutoResetEvent invalidatedEvt = new AutoResetEvent(false);
		readonly object sync = new object();

		DateRange timeLineRange;
		volatile bool isWorking;

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
	}
}
