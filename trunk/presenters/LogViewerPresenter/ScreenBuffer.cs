using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	// todo: concurentcy and reentry. one async op is allowed at a time.
	// consistency: maintened even if op is cancelled.
	// threading: must be called from one thread.
	// todo: tell this object about rawview to do messages stringification right.
	public interface IScreenBuffer
	{
		void SetSources(IEnumerable<IMessagesSource> sources);
		void SetViewSize(double sz);

		IEnumerable<ScreenBufferMessage> Messages { get; }
		IEnumerable<ScreenBufferMessage> ReversedMessages { get; }
		IEnumerable<SourceScreenBuffer> Sources { get; }
		double TopMessageScrolledLines { get; }
		BufferPositioningMethod PositioningMethod { get; set; }
		double BufferPosition { get; }

		Task MoveToStreamsBegin(
			CancellationToken cancellation
		);
		Task MoveToStreamsEnd(
			CancellationToken cancellation
		);
		Task MoveToDate( // todo: possibly not needed if there is navigation to bookmark
			DateTime dt,
			CancellationToken cancellation
		);
		Task<bool> MoveToMessage( // todo: refactor to accept bookmark
			MessageTimestamp dt,
			long position,
			string logSourceCollectionId,
			MessageMatchingMode mode,
			CancellationToken cancellation);
		Task<int> ShiftBy(
			double nrOfDisplayLines,
			CancellationToken cancellation
		);
		Task Reload(
			CancellationToken cancellation
		);
		Task MoveToPosition(
			double bufferPosition,
			CancellationToken cancellation
		);

		bool MakeFirstLineFullyVisible();
	};

	public enum MessageMatchingMode
	{
		ExactMatch,
		MatchNearest
	};

	public static class ScreenBufferExtensions
	{
		public static Task<bool> MoveToMessage(
			this IScreenBuffer sb,
			IMessage msg,
			MessageMatchingMode mode,
			CancellationToken cancellation)
		{
			return sb.MoveToMessage(msg.Time, msg.Position, msg.GetConnectionId(), mode, cancellation);
		}
	};

	public struct ScreenBufferMessage
	{
		public IMessage Message;
		public int Index;
		public IMessagesSource Source;
	};

	public struct SourceScreenBuffer
	{
		public IMessagesSource Source;
		public long Begin;
		public long End;
	};

	public enum BufferPositioningMethod
	{
		MessagePositions,
		MessageIndexes,
	};

	public class ScreenBuffer: IScreenBuffer
	{
		public ScreenBuffer()
		{
			buffers = new Dictionary<IMessagesSource, SourceBuffer>();
		}

		void IScreenBuffer.SetSources(IEnumerable<IMessagesSource> sources)
		{
			var newSources = sources.ToHashSet();
			foreach (var s in buffers.Keys.ToArray())
				if (!newSources.Contains(s))
					buffers.Remove(s);
			newSources.RemoveWhere(s => buffers.ContainsKey(s));

			// todo: if (screenBuffers.Count > 0) navigate newly added sources to current time.
			// keep selection.
			foreach (var s in newSources)
				buffers.Add(s, new SourceBuffer(s));

			if (buffers.Count == 0)
			{
				SetScrolledLines(0);
			}
		}

		BufferPositioningMethod IScreenBuffer.PositioningMethod
		{ 
			get { return positioningMethod; }
			set { positioningMethod = value; }
		}

		void IScreenBuffer.SetViewSize(double sz)
		{
			// todo: reload?
			viewSize = sz;
			bufferSize = (int) Math.Ceiling(viewSize + scrolledLines) + 1;
		}

		double IScreenBuffer.TopMessageScrolledLines 
		{
			get { return scrolledLines; }
		}

		bool IScreenBuffer.MakeFirstLineFullyVisible()
		{
			if (Math.Abs(scrolledLines) < 0.01)
				return false;
			SetScrolledLines(0);
			return true;
		}

		IEnumerable<ScreenBufferMessage> IScreenBuffer.Messages
		{
			get { return GetMessagesInternal().Forward(0, int.MaxValue).Select(ToScreenBufferMessage); }
		}

		IEnumerable<ScreenBufferMessage> IScreenBuffer.ReversedMessages
		{
			get { return GetMessagesInternal().Reverse(int.MaxValue, int.MinValue).Select(ToScreenBufferMessage); }
		}

		IEnumerable<SourceScreenBuffer> IScreenBuffer.Sources
		{
			get
			{
				return buffers.Select(b => new SourceScreenBuffer()
				{
					Source = b.Key, 
					Begin = b.Value.BeginPosition,
					End = b.Value.EndPosition
				});
			}
		}

		IMessagesCollection GetScreenBufferAsMessagesCollection()
		{
			return GetMessagesInternal();
		}

		static MessagesContainers.MergingCollection GetMessagesInternal(
			IEnumerable<SourceBuffer> sourceBuffers)
		{
			return new MessagesContainers.SimpleMergingCollection(sourceBuffers);
		}

		MessagesContainers.MergingCollection GetMessagesInternal()
		{
			return GetMessagesInternal(buffers.Values);
		}

		async Task IScreenBuffer.MoveToStreamsBegin(CancellationToken cancellation)
		{
			var tasks = buffers.Select(x => new
			{
				buf = x.Value,
				task = GetScreenBufferMessages(x.Key, x.Key.PositionsRange.Begin, 
					bufferSize, EnumMessagesFlag.Forward, cancellation)
			}).ToList();
			await Task.WhenAll(tasks.Select(x => x.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var x in tasks)
			{
				x.buf.Set(x.task.Result);
			}
			FinalizeSourceBuffers();
			SetScrolledLines(0);
		}

		Task IScreenBuffer.MoveToStreamsEnd(CancellationToken cancellation)
		{
			return MoveToStreamsEndInternal(cancellation);
		}

		async Task IScreenBuffer.MoveToDate(
			DateTime dt,
			CancellationToken cancellation
		)
		{
			var tasks = buffers.Select(s => new
			{
				buf = s.Value,
				task = GetScreenBufferMessages(s.Key, dt, bufferSize, cancellation),
			}).ToList();
			await Task.WhenAll(tasks.Select(i => i.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var t in tasks)
				t.buf.Set(t.task.Result);
			FinalizeSourceBuffers();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines(0);
		}

		async Task<bool> IScreenBuffer.MoveToMessage(
			MessageTimestamp dt,
			long position,
			string logSourceCollectionId,
			MessageMatchingMode mode,
			CancellationToken cancellation)
		{
			var tmp = buffers.ToDictionary(s => s.Key, s => new SourceBuffer(s.Value));
			var tasks = tmp.Select(s => new
			{
				buf = s.Value,
				task =
					(buffers.Count == 1 && mode == MessageMatchingMode.ExactMatch) ? 
						GetScreenBufferMessages(s.Key, position, bufferSize, EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, cancellation) :
						GetScreenBufferMessages(s.Key, dt.ToLocalDateTime(), bufferSize, cancellation),
			}).ToList();
			await Task.WhenAll(tasks.Select(i => i.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var t in tasks)
			{
				t.buf.Set(t.task.Result);
			}
			bool messageFound = false;
			foreach (var i in GetMessagesInternal(tmp.Values).Forward(0, int.MaxValue))
			{
				var cmp = MessagesComparer.CompareLogSourceConnectionIds(i.Message.Message.GetConnectionId(), logSourceCollectionId);
				if (cmp == 0)
					cmp = Math.Sign(i.Message.Message.Position - position);
				if (mode == MessageMatchingMode.ExactMatch)
					messageFound = cmp == 0;
				else if (mode == MessageMatchingMode.MatchNearest)
					messageFound = cmp > 0;
				if (messageFound)
					break;
				var sb = ((SourceBuffer)i.SourceCollection);
				sb.UnnededTopMessages++;
			}
			if (!messageFound)
			{
				if (mode == MessageMatchingMode.MatchNearest)
				{
					await MoveToStreamsEndInternal(cancellation);
					return true;
				}
				return false;
			}

			buffers = tmp;

			FinalizeSourceBuffers();

			if (AllLogsAreAtEnd())
			{
				await MoveToStreamsEndInternal(cancellation);
			}
			else
			{
				SetScrolledLines(0);
				var additionalSpace = ((int)Math.Floor(viewSize) - 1) / 2;
				if (additionalSpace > 0)
					await ShiftByInternal(-additionalSpace, cancellation);
			}

			return true;
		}

		Task<int> IScreenBuffer.ShiftBy(double nrOfDisplayLines, CancellationToken cancellation)
		{
			return ShiftByInternal(nrOfDisplayLines, cancellation);
		}

		void FinalizeSourceBuffers()
		{
			foreach (var buf in buffers.Values)
			{
				buf.Finalize(bufferSize);
			}
		}

		async Task IScreenBuffer.Reload(CancellationToken cancellation)
		{
			var tasks = buffers.Select(x => new
			{
				buf = x.Value,
				task = GetScreenBufferMessages(x.Key, x.Value.BeginPosition, bufferSize,
					EnumMessagesFlag.Forward, cancellation)
			}).ToList();
			await Task.WhenAll(tasks.Select(x => x.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var t in tasks)
				t.buf.Set(t.task.Result);
			FinalizeSourceBuffers();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
		}

		double IScreenBuffer.BufferPosition
		{
			get
			{
				if (positioningMethod == BufferPositioningMethod.MessagePositions)
					return GetPositionsBasedBufferPosition ();
				else if (positioningMethod == BufferPositioningMethod.MessageIndexes)
					return GetIndexesBasedBufferPosition ();
				else
					return 0;
			}
		}

		double GetPositionsBasedBufferPosition ()
		{
			long totalSourcesLength = 0;
			long currentSourcesPositionsSum = 0;
			foreach (var src in buffers.Values)
			{
				var srcRange = src.Source.PositionsRange;
				totalSourcesLength += srcRange.Length;
				currentSourcesPositionsSum += src.BeginPosition;
			}
			if (totalSourcesLength == 0)
				return 0;
			return (double)currentSourcesPositionsSum / (double)totalSourcesLength;
		}

		double GetIndexesBasedBufferPosition()
		{
			long totalSourcesLength = 0;
			long currentSourcesIndexesSum = 0;
			foreach (var src in buffers.Values)
			{
				var srcRange = src.Source.IndexesRange;
				totalSourcesLength += srcRange.Length;
				currentSourcesIndexesSum += src.BeginIndex.Value;
			}
			if (totalSourcesLength == 0)
				return 0;
			return (double)currentSourcesIndexesSum / (double)totalSourcesLength;
		}

		async Task IScreenBuffer.MoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			if (positioningMethod == BufferPositioningMethod.MessageIndexes)
				await IndexBasedMoveToPosition(position, cancellation);
			else if (positioningMethod == BufferPositioningMethod.MessagePositions)
				await PositionsBasedMoveToPosition (position, cancellation);
		}

		Task IndexBasedMoveToPosition (double position, CancellationToken cancellation)
		{
			return MoveToPositionCore(
				position, cancellation,
				src => src.IndexesRange.Length,
				rsp => rsp.Index.Value,
				() =>
				{
					long currentPos = 0;
					foreach (var buf in buffers.Values)
						if (buf.UnnededTopMessages < buf.Count)
							currentPos += buf.Get(buf.UnnededTopMessages).Index;
						else
							currentPos += buf.EndIndex.GetValueOrDefault();
					return currentPos;
				}
			);
		}

		Task PositionsBasedMoveToPosition (double position, CancellationToken cancellation)
		{
			if (buffers.Count == 1)
			{
				return SinlgeLogMoveToPosition(position, cancellation);
			}

			return MoveToPositionCore(
				position, cancellation,
				src => src.PositionsRange.Length,
				rsp => rsp.Position,
				() =>
				{
					long currentPos = 0;
					foreach (var buf in buffers.Values)
						if (buf.UnnededTopMessages < buf.Count)
							currentPos += buf.Get(buf.UnnededTopMessages).Message.Position;
						else
							currentPos += buf.EndPosition;
					return currentPos;
				}
			);
		}

		async Task SinlgeLogMoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			var buf = buffers.Values.Single();
			var pos = (long)(position * (double)buf.Source.PositionsRange.Length);
			var msgs = await GetScreenBufferMessages(buf.Source, pos, bufferSize, EnumMessagesFlag.Forward, cancellation);
			buf.Set(msgs);
			FinalizeSourceBuffers();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines(0);
		}

		async Task MoveToPositionCore (
			double position,
			CancellationToken cancellation,
			Func<IMessagesSource, long> getRangeLength,
			Func<DateBoundPositionResponseData, long> getDatePosition,
			Func<long> getCurrentFlatPosition
		)
		{
			var fullDatesRange = DateRange.MakeEmpty ();
			long fullPositionsRangeLength = 0;
			foreach (var s in buffers) {
				fullDatesRange = DateRange.Union (fullDatesRange, s.Key.DatesRange);
				fullPositionsRangeLength += getRangeLength(s.Key);
			}
			var flatLogPosition = position * (double)fullPositionsRangeLength;
			var searchRange = new ListUtils.VirtualList<DateTime> (
				(int)fullDatesRange.Length.TotalMilliseconds, i => fullDatesRange.Begin.AddMilliseconds (i));
			var ms = await searchRange.BinarySearchAsync (0, searchRange.Count, async d => 
			{
				long datePosition = 0;
				foreach (var b in buffers)
				{
					var dateBound = await b.Key.GetDateBoundPosition (
						d, ListUtils.ValueBound.Upper, LogProviderCommandPriority.RealtimeUserAction, cancellation);
					cancellation.ThrowIfCancellationRequested ();
					datePosition += getDatePosition(dateBound);
				}
				return datePosition < flatLogPosition;
			});
			cancellation.ThrowIfCancellationRequested ();
			var date = fullDatesRange.Begin.AddMilliseconds (ms);
			var tasks = buffers.Select (s => new {
				buf = s.Value,
				task = GetScreenBufferMessages (s.Key, date, bufferSize, cancellation)
			}).ToList ();
			await Task.WhenAll (tasks.Select (i => i.task));
			cancellation.ThrowIfCancellationRequested ();
			foreach (var t in tasks)
				t.buf.Set (t.task.Result);
			foreach (var m in GetMessagesInternal().Forward(0, int.MaxValue))
			{
				var messsageBuf = (SourceBuffer)m.SourceCollection;
				messsageBuf.UnnededTopMessages++;
				if (getCurrentFlatPosition() > flatLogPosition)
					break;
			}
			FinalizeSourceBuffers ();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines (0);
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferMessages(
			IMessagesSource src, long startFrom, int maxCount, EnumMessagesFlag flags, CancellationToken cancellation)
		{
			var backward = (flags & EnumMessagesFlag.Backward) != 0 ;
			var messages = new List<IndexedMessage>();
			await src.EnumMessages(startFrom, msg => 
			{
				messages.Add(msg);
				return messages.Count < (maxCount + (backward ? 0 : 1));
			}, flags | EnumMessagesFlag.IsActiveLogPositionHint, LogProviderCommandPriority.RealtimeUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			var firstRead = messages.FirstOrDefault();
			if (backward)
				messages.Reverse();
			var badPosition = backward ? src.PositionsRange.Begin : src.PositionsRange.End;
			ScreenBufferMessagesRange ret;
			ret.Messages = messages;
			ret.BeginPosition = messages.Count > 0 ? messages[0].Message.Position : badPosition;
			if (backward)
				ret.EndPosition = messages.Count > 0 ? startFrom : badPosition;
			else
				ret.EndPosition = messages.Count > maxCount ? messages[maxCount].Message.Position : badPosition;
			var idxRange = src.IndexesRange;
			var badIndex = (int)(backward ? idxRange.Begin : idxRange.End);
			ret.BeginIndex = messages.Count > 0 ? messages[0].Index : badIndex;
			if (backward)
				ret.EndIndex = firstRead.Message != null ? (firstRead.Index + 1) : badIndex;
			else
				ret.EndIndex = messages.Count > maxCount ? messages[maxCount].Index : badIndex;
			if (messages.Count > maxCount && !backward)
			{
				Debug.Assert(messages.Count == maxCount + 1);
				messages.RemoveRange(maxCount, messages.Count - maxCount);
			}
			return ret;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferMessages(
			IMessagesSource src, DateTime dt, int maxCount, CancellationToken cancellation)
		{
			var startFrom = await src.GetDateBoundPosition(dt, ListUtils.ValueBound.Lower, 
				LogProviderCommandPriority.RealtimeUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			var messages = new List<IndexedMessage>();
			var additionalMessagesCount = 0;
			await src.EnumMessages(
				startFrom.Position,
				msg => 
				{
					messages.Add(msg);
					var pastRequestedTime = msg.Message.Time.ToLocalDateTime() > dt;
					if (!pastRequestedTime)
						return true;
					++additionalMessagesCount;
					return additionalMessagesCount < (maxCount + 1); // +1 to collect past-the-end message
				}, 
				EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, 
				LogProviderCommandPriority.RealtimeUserAction,
				cancellation
			);
			cancellation.ThrowIfCancellationRequested();
			var srcPositionsRange = src.PositionsRange;
			ScreenBufferMessagesRange ret;
			ret.Messages = messages;
			ret.BeginPosition = messages.Count > 0 ? messages[0].Message.Position : srcPositionsRange.End;
			if (additionalMessagesCount > maxCount) // past-the-end message was collected
			{
				ret.EndPosition = messages[messages.Count - 1].Message.Position;
				messages.RemoveRange(messages.Count - 1, 1);
			}
			else
			{
				ret.EndPosition = srcPositionsRange.End;
			}
			var idxRange = src.IndexesRange;
			if (!idxRange.IsEmpty)
			{
				ret.BeginIndex = messages.Count > 0 ? messages[0].Index : (int)idxRange.End;
				ret.EndIndex = messages.Count > 0 ? messages[messages.Count - 1].Index : (int)idxRange.End;
			}
			else
			{
				ret.BeginIndex = ret.EndIndex = null;
			}
			return ret;
		}

		struct ScreenBufferMessagesRange
		{
			public List<IndexedMessage> Messages;
			public long BeginPosition, EndPosition;
			public int? BeginIndex, EndIndex;
		};

		class SourceBuffer: IMessagesCollection
		{
			public SourceBuffer(IMessagesSource src)
			{
				this.source = src;
			}

			public SourceBuffer(SourceBuffer other)
			{
				this.source = other.source;
				this.beginPosition = other.beginPosition;
				this.endPosition = other.endPosition;
				this.beginIndex = other.beginIndex;
				this.endIndex = other.endIndex;
				this.messages.AddRange(other.messages);
			}

			public IMessagesSource Source { get { return source; } }

			/// Position of the first message in the buffer 
			/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
			/// currently viewed time respectively.
			public long BeginPosition { get { return beginPosition; } }
			/// Position of the message following the last message in the buffer
			/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
			/// currently viewed time respectively.
			public long EndPosition { get { return endPosition; } }

			public int? BeginIndex { get { return beginIndex; } }
			public int? EndIndex { get { return endIndex; } }

			// todo: hide public member
			public int UnnededTopMessages;

			public void Set(ScreenBufferMessagesRange range)
			{
				messages.Clear();
				messages.AddRange(range.Messages);
				beginPosition = range.BeginPosition;
				endPosition = range.EndPosition;
				beginIndex = range.BeginIndex;
				endIndex = range.EndIndex;
			}

			public void Append(ScreenBufferMessagesRange range)
			{
				Debug.Assert(endPosition == range.BeginPosition);
				messages.AddRange(range.Messages);
				endPosition = range.EndPosition;
				endIndex = range.EndIndex;
			}

			public void Prepend(ScreenBufferMessagesRange range)
			{
				Debug.Assert(beginPosition == range.EndPosition);
				messages.InsertRange(0, range.Messages);
				beginPosition = range.BeginPosition;
				beginIndex = range.BeginIndex;
			}

			public void Finalize(int maxSz)
			{
				if (UnnededTopMessages != 0)
				{
					if (messages.Count > UnnededTopMessages)
					{
						var newBeginMsg = messages[UnnededTopMessages];
						beginPosition = newBeginMsg.Message.Position;
						beginIndex = newBeginMsg.Index;
					}
					else
					{
						beginPosition = endPosition;
						beginIndex = endIndex;
					}
					messages.RemoveRange(0, UnnededTopMessages);
					UnnededTopMessages = 0;
				}
				if (messages.Count > maxSz)
				{
					var pastTheEndMsg = messages[maxSz];
					endPosition = pastTheEndMsg.Message.Position;
					endIndex = pastTheEndMsg.Index;
					messages.RemoveRange(maxSz, messages.Count - maxSz);
				}
			}

			IEnumerable<IndexedMessage> IMessagesCollection.Forward (int begin, int end)
			{
				for (var i = begin; i != end; ++i)
					yield return new IndexedMessage(i, messages[i].Message);
			}

			IEnumerable<IndexedMessage> IMessagesCollection.Reverse (int begin, int end)
			{
				for (var i = begin; i != end; --i)
					yield return new IndexedMessage(i, messages[i].Message);
			}

			int IMessagesCollection.Count
			{
				get { return messages.Count; }
			}

			public int Count
			{
				get { return messages.Count; }
			}

			public IndexedMessage Get(int idx)
			{
				return messages[idx];
			}

			readonly List<IndexedMessage> messages = new List<IndexedMessage>();
			readonly IMessagesSource source;
			long beginPosition;
			long endPosition;
			int? beginIndex, endIndex;
		};

		void SetScrolledLines(double value)
		{
			scrolledLines = value;
		}

		bool AllLogsAreAtEnd()
		{
			//return false;
			return buffers.All(b => b.Value.EndPosition == b.Key.PositionsRange.End);
		}

		async Task MoveToStreamsEndInternal(CancellationToken cancellation)
		{
			var tasks = buffers.Select(x => new
			{
				buf = x.Value,
				task = GetScreenBufferMessages(x.Key, x.Key.PositionsRange.End, bufferSize, EnumMessagesFlag.Backward, cancellation)
			}).ToList();
			await Task.WhenAll(tasks.Select(x => x.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var x in tasks)
			{
				x.buf.Set(x.task.Result);
			}
			double viewSizeRemainder = viewSize;
			bool removingUnnededTopMessages = false;
			foreach (var x in GetMessagesInternal().Reverse(int.MaxValue, int.MinValue))
			{
				var sb = (SourceBuffer)x.SourceCollection;
				double linesCount = x.Message.Message.GetLinesCount();
				if (removingUnnededTopMessages)
				{
					sb.UnnededTopMessages++;
				}
				else if (linesCount > viewSizeRemainder)
				{
					SetScrolledLines(linesCount - viewSizeRemainder);
					removingUnnededTopMessages = true;
				}
				else
				{
					viewSizeRemainder -= linesCount;
				}
			}
			FinalizeSourceBuffers();
		}

		async Task<int> ShiftByInternal(double nrOfDisplayLines, CancellationToken cancellation)
		{
			if (buffers.Count == 0)
				return 0;

			var newScrolledLines = scrolledLines + nrOfDisplayLines;
			var shiftByDisplayLines = (int)Math.Floor(newScrolledLines);
			var shiftByDisplayLinesBk = shiftByDisplayLines;
			var currentTop = GetScreenBufferAsMessagesCollection().Forward(0, 1).FirstOrDefault();
			int nrOfMessagesToLoad = 0;
			if (shiftByDisplayLines < 0)
			{
				nrOfMessagesToLoad = -bufferSize;
			}
			else if (shiftByDisplayLines > 0)
			{
				foreach (var m in GetScreenBufferAsMessagesCollection().Forward(0, bufferSize))
				{
					var linesCount = m.Message.GetLinesCount();
					if (shiftByDisplayLines < linesCount)
						break;
					++nrOfMessagesToLoad;
					newScrolledLines -= linesCount;
					shiftByDisplayLines -= linesCount;
				}
			}
			if (nrOfMessagesToLoad == 0)
			{
				SetScrolledLines(newScrolledLines);
				return 0;
			}

			Debug.Assert(Math.Abs(nrOfMessagesToLoad) < bufferSize);
			Debug.Assert(currentTop.Message != null);
			var sourcesDict = buffers.Values.Select(s => new
			{
				buf = s,
				loadTask = GetScreenBufferMessages(
					s.Source,
					nrOfMessagesToLoad < 0 ? s.BeginPosition : s.EndPosition,
					Math.Abs(nrOfMessagesToLoad),
					nrOfMessagesToLoad < 0 ? EnumMessagesFlag.Backward : EnumMessagesFlag.Forward,
					cancellation
				)
			}).ToList();
			await Task.WhenAll(sourcesDict.Select(t => t.loadTask));
			cancellation.ThrowIfCancellationRequested();

			// todo: do not change screen buffer until transaction is confirmed

			int loadedMessages = 0;
			foreach (var src in sourcesDict)
			{
				var list = src.loadTask.Result;
				loadedMessages += list.Messages.Count;
				if (nrOfMessagesToLoad < 0)
					src.buf.Prepend(list);
				else
					src.buf.Append(list);
			}

			int newTopDisplayIndex =
				GetScreenBufferAsMessagesCollection()
				.Forward(0, int.MaxValue)
				.Where(i => i.Message == currentTop.Message)
				.Select(i => i.Index)
				.FirstOrDefault(-1);
			Debug.Assert(newTopDisplayIndex >= 0);

			int shiftedBy;

			if (nrOfMessagesToLoad > 0)
			{
				foreach (var i in GetMessagesInternal().Forward(0, newTopDisplayIndex + nrOfMessagesToLoad))
					((SourceBuffer)i.SourceCollection).UnnededTopMessages++;
				shiftedBy = nrOfMessagesToLoad;
			}
			else
			{
				shiftedBy = 0;
				bool markingUnneeded = false;
				foreach (var i in GetMessagesInternal().Reverse(newTopDisplayIndex - 1, int.MinValue))
				{
					if (markingUnneeded)
					{
						((SourceBuffer)i.SourceCollection).UnnededTopMessages++;
					}
					else
					{
						var linesCount = i.Message.Message.GetLinesCount();
						shiftByDisplayLines += linesCount;
						newScrolledLines += linesCount;
						shiftedBy -= linesCount;
						if (shiftByDisplayLines >= 0)
							markingUnneeded = true;
					}
				}
				if (shiftByDisplayLinesBk < shiftedBy)
				{
					newScrolledLines = 0;
				}
			}

			FinalizeSourceBuffers();

			if (shiftByDisplayLinesBk > 0 && AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines(newScrolledLines);

			return shiftedBy;
		}

		static ScreenBufferMessage ToScreenBufferMessage(MessagesContainers.MergingCollectionEntry m)
		{
			return new ScreenBufferMessage()
			{
				Message = m.Message.Message,
				Index = m.Message.Index,
				Source = ((SourceBuffer)m.SourceCollection).Source
			};
		}

		Dictionary<IMessagesSource, SourceBuffer> buffers;
		BufferPositioningMethod positioningMethod;
		double viewSize; // size of the view the screen buffer needs to fill. nr of lines.
		int bufferSize; // size of the buffer. it has enought messages to fill the view of size viewSize.
		double scrolledLines; // scrolling positon as nr of lines.
	};
};