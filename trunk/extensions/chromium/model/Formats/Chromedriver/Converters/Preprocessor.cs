using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using LogJoint.Preprocessing;
using LogJoint.Analytics;
using System.Threading;

namespace LogJoint.Chromium.ChromeDriver
{
	public class TimeFixerPreprocessingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal static readonly string stepName = "chrome_debug.fix_time";
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly PreprocessingStepParams sourceFile;
		readonly ILogProviderFactory chromeDriverLogsFactory;

		internal TimeFixerPreprocessingStep(
			IPreprocessingStepsFactory preprocessingStepsFactory,
			ILogProviderFactory chromeDriverLogsFactory,
			PreprocessingStepParams srcFile
		)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.sourceFile = srcFile;
			this.chromeDriverLogsFactory = chromeDriverLogsFactory;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, p =>
			{
				var cp = ((IFileBasedLogProviderFactory)chromeDriverLogsFactory).CreateParams(p.Uri);
				p.DumpToConnectionParams(cp);
				callback.YieldLogProvider(new YieldedProvider() {
					Factory = chromeDriverLogsFactory,
					ConnectionParams = cp,
					DisplayName = p.DisplayName,
				}); 
			});
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, x => { ret = x; });
			return ret;
		}

		async Task ExecuteInternal(IPreprocessingStepCallback callback, Action<PreprocessingStepParams> onNext)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			await (new Writer()).Write(
				() => new FileStream(tmpFileName, FileMode.Create),
				s => s.Dispose(), 
				FixTimestamps((new Reader(callback.Cancellation)).Read(
					sourceFile.Uri,
					progressHandler: prct => callback.SetStepDescription(
						string.Format("{0}: fixing timestamps {1}%", sourceFile.FullPath, (int)(prct * 100)))
				))
			);

			onNext(new PreprocessingStepParams(tmpFileName, string.Format("{0}\\with_fixed_timestamps", sourceFile.FullPath),
				Utils.Concat(sourceFile.PreprocessingSteps, stepName)));
		}

		class MessageEntry
		{
			public Message Msg { get; }
			public double? Timestamp { get; }
			public double? WallTime { get; }
			public bool IsInvalidated { get; private set; }

			public string RequestId { get; }
			public EntryType Type { get; }
			public bool IsServedFromCache { get; set; }

			public double ResponseTiming_RequestTime { get; }

			public enum EntryType
			{
				Unspecified,
				StartRequest,
				ResponseWithTiming,
				ServedFromCache
			};

			public MessageEntry(Message m)
			{
				Msg = m;
				var parsed = DevTools.Events.LogMessage.Parse(m.Text);
				if (parsed != null)
				{
					var payoad = parsed.ParsePayload<DevTools.Events.TimeStampsInfo>();
					if (payoad != null)
					{
						this.Timestamp = payoad.timestamp;
						this.WallTime = payoad.wallTime;
						this.RequestId = payoad.requestId;
						if (this.RequestId != null)
						{
							if (parsed.EventType == DevTools.Events.Network.RequestWillBeSent.EventType)
							{
								this.Type = EntryType.StartRequest;
							}
							else if (parsed.EventType == DevTools.Events.Network.ResponseReceived.EventType)
							{
								var timing = payoad.response?.timing;
								if (timing?.requestTime != null)
								{
									this.Type = EntryType.ResponseWithTiming;
									this.ResponseTiming_RequestTime = timing.requestTime.Value;
								}
							}
							else if (parsed.EventType == DevTools.Events.Network.RequestServedFromCache.EventType)
							{
								this.Type = EntryType.ServedFromCache;
							}
						}
					}
				}
			}

			public MessageEntry Rebase(DateTime timestampBase)
			{
				return Timestamp == null ? this : new MessageEntry(this, timestampBase);
			}

			public MessageEntry InvalidateAndMakeFixedStartRequest(double responseTimingRequestTime)
			{
				this.IsInvalidated = true;
				return new MessageEntry(this, responseTimingRequestTime);
			}

			MessageEntry(MessageEntry original, double responseTimingRequestTime)
			{
				this.Msg = original.Msg;
				this.Type = original.Type;
				this.Timestamp = responseTimingRequestTime;
				this.RequestId = original.RequestId;
				this.IsServedFromCache = original.IsServedFromCache;
			}

			MessageEntry(MessageEntry original, DateTime timestampBase)
			{
				var origTs = original.Timestamp.Value;
				DateTime ts;
				if (origTs > 1520000000000)
					ts = TimeUtils.UnixTimestampMillisToDateTime(origTs).ToUnspecifiedTime();
				else
					ts = timestampBase.AddSeconds(origTs);
				this.Msg = new Message(
					original.Msg.Index,
					original.Msg.StreamPosition,
					ts,
					original.Msg.Severity, 
					original.Msg.Text
				);
				this.Timestamp = original.Timestamp;
				this.RequestId = original.RequestId;
				this.Type = original.Type;
				this.IsServedFromCache = original.IsServedFromCache;
			}
		};

		/// <summary>
		/// Makes sure events have correct timestamp and go in correct order.
		/// Chromedriver log has a problem that timestamps at beginning of lines like [1525678451.879]
		/// seem to be rounded up to next 100ms boundary which results to inaccurate views.
		/// Most important messages like Network.requestWillBeSent have "timestamp" as a json field.
		/// That "timestamp" is nr os seconds from unknown origin. Luckily some messages also have 
		/// "wallTime" in json payload that can help interpret "timestamp".
		/// Another problem is that "timestamp" of Network.requestWillBeSent might not match
		/// timing.requestTime in Network.responseReceived. The latter seems to be more accurate.
		/// However if a request is served from cache its timing.requestTime is totally wrong and 
		/// should be ignored in favor of  Network.requestWillBeSent's "timestamp".
		/// </summary>
		static IEnumerableAsync<Message[]> FixTimestamps(IEnumerableAsync<Message[]> messages)
		{
			DateTime? timestampBase = null;
			var pendingMessages = new List<MessageEntry>();
			var queue = new VCSKicksCollection.PriorityQueue<MessageEntry>(new Comparer());
			var queuedRequestStarts = new Dictionary<string, MessageEntry>();
			double? lastDequeuedTimestamp = null;
			Action<Queue<Message>> dequeue = (outputQueue) =>
			{
				var entry = queue.Dequeue();
				if (!entry.IsInvalidated)
				{
					if (lastDequeuedTimestamp == null || entry.Timestamp != null)
						lastDequeuedTimestamp = entry.Timestamp;
					outputQueue.Enqueue(entry.Msg);
					if (entry.Type == MessageEntry.EntryType.StartRequest)
						queuedRequestStarts.Remove(entry.RequestId);
				}
			};
			Action<MessageEntry> enqueue = null;
			enqueue = (r) => 
			{
				var rebased = r.Rebase(timestampBase.Value);
				queue.Enqueue(rebased);
				if (r.Type == MessageEntry.EntryType.StartRequest)
				{
					queuedRequestStarts[r.RequestId] = rebased;
				}
				else if (r.Type == MessageEntry.EntryType.ResponseWithTiming)
				{
					MessageEntry queuedRequest;
					if (queuedRequestStarts.TryGetValue(r.RequestId, out queuedRequest) && !queuedRequest.IsInvalidated && !queuedRequest.IsServedFromCache)
					{
						if (lastDequeuedTimestamp == null || r.ResponseTiming_RequestTime > lastDequeuedTimestamp.Value)
						{
							enqueue(queuedRequest.InvalidateAndMakeFixedStartRequest(r.ResponseTiming_RequestTime));
						}
					}
				}
				else if (r.Type == MessageEntry.EntryType.ServedFromCache)
				{
					MessageEntry queuedRequest;
					if (queuedRequestStarts.TryGetValue(r.RequestId, out queuedRequest))
					{
						queuedRequest.IsServedFromCache = true;
					}
				}
			};
			Action flushPendingMessages = () => 
			{
				foreach (var pendingMessage in pendingMessages)
					enqueue(pendingMessage);
				pendingMessages.Clear();
			};
			int queueSize = 4096;
			Action<Message[], Queue<Message>> selector = (batch, outputQueue) =>
			{
				foreach (var newEntry in batch.AsParallel().AsOrdered().Select(m => new MessageEntry(m)))
				{
					if (timestampBase == null && newEntry.Timestamp != null && newEntry.WallTime != null)
					{
						timestampBase = TimeUtils.UnixTimestampMillisToDateTime(
							newEntry.WallTime.Value * 1000d).ToUnspecifiedTime().AddSeconds(
								-newEntry.Timestamp.Value);
						flushPendingMessages();
					}

					if (timestampBase == null)
						pendingMessages.Add(newEntry);
					else
						enqueue(newEntry);

					if (queue.Count >= queueSize * 2)
					{
						while (queue.Count > queueSize)
							dequeue(outputQueue);
					}
				}
			};
			return messages.Select<Message, Message>(
				selector,
				(outputQueue) =>
				{
					while (queue.Count > 0)
						dequeue(outputQueue);
				}
			);
		}

		class Comparer : IComparer<MessageEntry>
		{
			int IComparer<MessageEntry>.Compare(MessageEntry x, MessageEntry y)
			{
				int i = Math.Sign(x.Msg.Timestamp.Ticks - y.Msg.Timestamp.Ticks);
				if (i != 0)
					return i;
				return x.Msg.Index - y.Msg.Index;
			}
		}
	};
}
