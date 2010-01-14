using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace LogJoint
{
	public interface ITimeGapsHost
	{
		Source Tracer { get; }
		ISynchronizeInvoke Invoker { get; }
		IEnumerable<ILogSource> Sources { get; }
	};

	public struct TimeGap
	{
		public DateRange Range { get { return range; } }
		public TimeGap(DateRange r) 
		{ 
			range = r;
		}
		public override string ToString()
		{
			return string.Format("TimeGap ({0}) - ({1})", range.Begin, range.End);
		}
		DateRange range;
	};

	/// <summary>
	/// This class implements the logic of finding the gaps (periods of time where there are no messages)
	/// of the timeline.
	/// </summary>
	/// <remarks>
	/// This class starts to work when a client calls Update(DateRange) method. The value passed
	/// to Update() is a dare range there the client wants to find the gaps on. The dates range
	/// is divided to a fixed number of pieces. The length of the piece is used as a threshold.
	/// The periods of time with no messages and with the lenght greated than the threshold are
	/// considered as time gaps.
	/// </remarks>
	public class TimeGaps: IDisposable
	{
		public TimeGaps(ITimeGapsHost host)
		{
			using (host.Tracer.NewFrame)
			{
				this.host = host;
				this.trace = host.Tracer;
				this.syncInvoke = host.Invoker;

				thread = new Thread(ThreadProc);
				thread.Priority = ThreadPriority.BelowNormal;
				thread.Name = "TimeGaps working thread";
				trace.Info("Startin working thread");
				thread.Start();
			}
		}

		public void Dispose()
		{
			using (trace.NewFrame)
			{
				trace.Info("Setting stop event");
				stopEvt.Set();
				trace.Info("Waiting for the thread to complete");
				thread.Join();
				trace.Info("Working thread finished");
			}
		}

		public void Update(DateRange r)
		{
			using (trace.NewFrame)
			{
				trace.Info("Time range passed: {0}", r);

				bool invalidate = false;

				lock (sync)
				{
					if (Abs(timeLineRange.Begin - r.Begin) > gaps.Threshold
					 || Abs(timeLineRange.End - r.End) > gaps.Threshold)
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

		public event EventHandler OnTimeGapsChanged;

		public IList<TimeGap> Gaps
		{
			get 
			{
				lock (sync)
				{
					return gaps.Items;
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
							Refresh();

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

		class Helper: IDisposable
		{
			class SourceStruct
			{
				public long CurrentPosition = long.MinValue;
				public bool IsHandled = false;
			};

			readonly TimeGaps owner;
			readonly ISynchronizeInvoke invoke;
			readonly Source trace;
			readonly ManualResetEvent allReadersReturned = new ManualResetEvent(false);
			readonly Dictionary<ILogReader, SourceStruct> sources = new Dictionary<ILogReader, SourceStruct>();
			static readonly object[] emptyArgs = new object[] { };
			readonly ReaderWriterLock sync = new ReaderWriterLock();
			readonly CompletionHandler completion;

			int readersToWait = 0;
			int readersAdvanced = 0;
			DateTime currentDate = DateTime.MinValue;
			bool lowerBoundMode = false;
			bool isDisposed;

			public Helper(TimeGaps owner)
			{
				this.owner = owner;
				this.invoke = owner.syncInvoke;
				this.trace = owner.trace;
				this.completion = CompletionHandler;
			}

			public void Dispose()
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

			public int ReadSources()
			{
				using (trace.NewFrame)
				{
					trace.Info("Getting the list of log sources (sumbitting the request to the main thread)");
					ITimeGapsHost host = owner.host;
					IAsyncResult ar = invoke.BeginInvoke((SimpleDelegate)delegate()
					{
						using (trace.NewFrame)
						{
							trace.Info("Getting the list of log sources");
							foreach (ILogSource src in host.Sources)
							{
								sources[src.Reader] = new SourceStruct();
								trace.Info("---> found log source: id={0}, name={1}", src.Reader.GetHashCode(), src.DisplayName);
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

			public bool MoveToDateBound(DateTime d, bool lowerBound)
			{
				using (trace.NewFrame)
				{
					trace.Info("Moving to the date {0} than '{1}' by sending 'get {2} bound' request to all readers", 
						lowerBound ? "less" : "greater", d, lowerBound ? "lower" : "lower (rev)");

					trace.Info("Resetting the counters");
					readersToWait = sources.Count;
					allReadersReturned.Reset();
					readersAdvanced = 0;
					lowerBoundMode = lowerBound;
					if (lowerBound)
						currentDate = DateTime.MinValue;
					else
						currentDate = DateTime.MaxValue;

					foreach (SourceStruct src in sources.Values)
					{
						src.IsHandled = false;
					}

					int readersToHandle = sources.Count;
					for (int iteration = 0; ; ++iteration)
					{
						trace.Info("It's iteration {0} of trying to send the request to all readers", iteration);
						IAsyncResult ar = invoke.BeginInvoke((SimpleDelegate)delegate()
						{
							using (trace.NewFrame)
							{
								trace.Info("Sending the request to all readers");
								foreach (KeyValuePair<ILogReader, SourceStruct> src in sources)
								{
									trace.Info("---> {0}", src.Key.GetHashCode());
									if (src.Value.IsHandled)
									{
										trace.Info("Already handled. Continuing.");
										continue;
									}
									if (!src.Key.WaitForIdleState(0))
									{
										trace.Info("The reader if busy. Continuing with other readers.");
										continue;
									}
									try
									{
										trace.Info("The reader is idling. Sending the request.");
										if (lowerBound)
											src.Key.GetDateBoundPosition(d, PositionedMessagesUtils.ValueBound.Lower, completion);
										else
											src.Key.GetDateBoundPosition(d, PositionedMessagesUtils.ValueBound.LowerReversed, completion);
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

					trace.Info("Waiting for the responces from all readers");
					if (owner.CheckEvents(sources.Count * 3000, new WaitHandle[] { allReadersReturned }) == WaitHandle.WaitTimeout)
					{
						trace.Warning("Some of the readers didn't respond ({0}). Giving up by throwing InvalidateException.", readersToWait);
						throw new InvalidateException();
					}

					bool ret = readersAdvanced != 0;

					trace.Info("Readers that have advanced their positions: {0}; returning {1}", readersAdvanced, ret);

					return ret;
				}
			}

			public DateTime CurrentDate
			{
				get { return currentDate; }
			}

			void CompletionHandler(ILogReader reader, object result)
			{
				using (trace.NewFrame)
				{
					KeyValuePair<long, DateTime> res = (KeyValuePair<long, DateTime>)result;
					trace.Info("Reader {0} returned ({1}, {2})", reader.GetHashCode(), res.Key, res.Value);
					
					SourceStruct src;

					sync.AcquireReaderLock(Timeout.Infinite);
					try
					{
						if (isDisposed)
						{
							trace.Warning("The helper object is already disposed. Ignoring this completion call.");
							return;
						}
						src = sources[reader];
					}
					finally
					{
						sync.ReleaseReaderLock();
					}

					trace.Info("Reader's current position: {0}", src.CurrentPosition);

					if (res.Key > src.CurrentPosition)
					{
						trace.Info("Reader has advanced its position: {0}", res.Key);
						Interlocked.Increment(ref readersAdvanced);
						src.CurrentPosition = res.Key;
					}

					bool advanceDate;
					if (lowerBoundMode)
						advanceDate = res.Value > currentDate;
					else
						advanceDate = res.Value < currentDate;

					if (advanceDate)
					{
						trace.Info("Reader has advanced the current date: {0}", res.Value);
						currentDate = res.Value;
					}

					if (Interlocked.Decrement(ref readersToWait) == 0)
					{
						trace.Info("All readers have returned a value. This was a last completion call. Setting completion event");
						allReadersReturned.Set();
					}
				}
			}


		};

		void SetNewGaps(GapsCache gaps)
		{
			lock (sync)
			{
				this.gaps = gaps;
			}
			trace.Info("Posting OnTimeGapsChanged event");
			syncInvoke.BeginInvoke(OnTimeGapsChanged, new object[] { this, EventArgs.Empty });
		}

		void Refresh()
		{
			using (trace.NewFrame)
			{
				List<TimeGap> ret = new List<TimeGap>();

				DateRange range;
				lock (sync)
				{
					range = timeLineRange;
				}
				trace.Info("Time line dates range: {0}", range.ToString());

				TimeSpan threshold = TimeSpan.FromMilliseconds(range.Length.TotalMilliseconds / 10.0);
				trace.Info("Threshold={0}", threshold.ToString());

				using (Helper helper = new Helper(this))
				{

					if (helper.ReadSources() == 0)
					{
						trace.Info("No log sources found.");
						SetNewGaps(new GapsCache());
						return;
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
					for (DateTime d = range.Begin; d < range.End; )
					{
						trace.Info("Moving to the lower bound of {0}", d);

						if (helper.MoveToDateBound(d, true))
						{
							trace.Info("Moved successfully. The lower bound is {0}.", helper.CurrentDate);
							d = helper.CurrentDate + threshold;
						}
						else
						{
							DateTime gapStart = helper.CurrentDate;
							trace.Info("No readers advanced. It's time gap starting at {0}", gapStart);
							
							trace.Info("Moving to the date greater than {0}", d);
							helper.MoveToDateBound(d, false);

							DateTime gapEnd = helper.CurrentDate;
							trace.Info("The end of the gap: {0}", gapEnd);

							d = helper.CurrentDate + threshold;

							TimeGap gap = new TimeGap(new DateRange(gapStart, gapEnd));
							trace.Info("Creating new gap {0}", gap);

							ret.Add(gap);
						}
					}
				}

				trace.Info("Returning {0} pags", ret.Count);

				GapsCache tmp = new GapsCache();
				tmp.Items = ret.ToArray();
				tmp.Range = range;
				tmp.Threshold = threshold;

				SetNewGaps(tmp);
			}
		}




		readonly ITimeGapsHost host;
		readonly Source trace;
		readonly ISynchronizeInvoke syncInvoke;
		readonly Thread thread;
		readonly ManualResetEvent stopEvt = new ManualResetEvent(false);
		readonly AutoResetEvent invalidatedEvt = new AutoResetEvent(false);
		readonly object sync = new object();

		DateRange timeLineRange;

		class GapsCache
		{
			static readonly TimeGap[] Empty = new TimeGap[] { };

			public TimeGap[] Items = Empty;
			public DateRange Range;
			public TimeSpan Threshold;
		};

		GapsCache gaps = new GapsCache();
	}
}
