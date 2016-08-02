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

	public interface IScrollPositioningDataProvider
	{
		long GetScrollRangeLength(IMessagesSource src);
		long MapPositionToScrollPosition(IMessagesSource src, long pos);
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
		public int LineIndex;
		public int Index;
		public IMessagesSource Source;
	};

	public struct SourceScreenBuffer
	{
		public IMessagesSource Source;
		public long Begin;
		public long End;
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

		IEnumerable<DisplayLine> EnumScreenBufferLines()
		{
			return GetMessagesInternal().Forward(0, int.MaxValue).Select(m => ((SourceBuffer)m.SourceCollection).Get(m.SourceIndex).MakeIndexed(m.Message.Index));
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
				task = GetScreenBufferLines(x.Key, x.Key.PositionsRange.Begin, 
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
				task = GetScreenBufferLines(s.Key, dt, bufferSize, cancellation),
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
						GetScreenBufferLines(s.Key, position, bufferSize, EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, cancellation) :
						GetScreenBufferLines(s.Key, dt.ToLocalDateTime(), bufferSize, cancellation),
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
				task = GetScreenBufferLines(x.Key, x.Value.BeginPosition, bufferSize,
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
				long totalSourcesLength = 0;
				long currentSourcesPositionsSum = 0;
				foreach (var src in buffers.Values)
				{
					totalSourcesLength += src.Source.ScrollPositionsRange.Length;
					currentSourcesPositionsSum +=
						src.Count > 0 ?
							GetLineScrollPosition(src.Source, src.Get(0)) : 
							src.Source.MapPositionToScrollPosition(src.BeginPosition);
				}
				if (totalSourcesLength == 0)
					return 0;
				return (double)currentSourcesPositionsSum / (double)totalSourcesLength;
			}
		}

		long GetLineScrollPosition(IMessagesSource src, DisplayLine dl) // todo: move to private funcs section
		{
			return src.MapPositionToScrollPosition(dl.Message.Position) + (dl.LinePosition - dl.Message.Position);
		}

		Task IScreenBuffer.MoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			if (buffers.Count == 1)
				return SinlgeLogMoveToPosition(position, cancellation);
			return MoveToPositionCore(position, cancellation);
		}

		async Task SinlgeLogMoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			var buf = buffers.Values.Single();
			var scrollPos = (long)(position * (double)buf.Source.ScrollPositionsRange.Length);
			var absolutePos = buf.Source.MapScrollPositionToPosition(scrollPos);
			var msgs = await GetScreenBufferLines(buf.Source, absolutePos, bufferSize, EnumMessagesFlag.Forward, cancellation);
			buf.Set(msgs);
			foreach (var m in GetMessagesInternal().Forward(0, int.MaxValue))
			{
				((SourceBuffer)m.SourceCollection).UnnededTopMessages++;
				if (CalcScrollPosHelper() > scrollPos)
					break;
			}
			FinalizeSourceBuffers();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines(0);
		}

		async Task MoveToPositionCore (
			double position,
			CancellationToken cancellation
		)
		{
			var fullDatesRange = DateRange.MakeEmpty ();
			long fullPositionsRangeLength = 0;
			foreach (var s in buffers) {
				fullDatesRange = DateRange.Union (fullDatesRange, s.Key.DatesRange);
				fullPositionsRangeLength += s.Key.ScrollPositionsRange.Length;
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
					datePosition += b.Key.MapPositionToScrollPosition(dateBound.Position); //todo: consider changing GetDateBoundPosition retval to include scrollpos
				}
				return datePosition < flatLogPosition;
			});
			cancellation.ThrowIfCancellationRequested ();
			var date = fullDatesRange.Begin.AddMilliseconds (ms);
			var tasks = buffers.Select (s => new {
				buf = s.Value,
				task = GetScreenBufferLines (s.Key, date, bufferSize, cancellation)
			}).ToList ();
			await Task.WhenAll (tasks.Select (i => i.task));
			cancellation.ThrowIfCancellationRequested ();
			foreach (var t in tasks)
				t.buf.Set (t.task.Result);
			foreach (var m in GetMessagesInternal().Forward(0, int.MaxValue))
			{
				var messsageBuf = (SourceBuffer)m.SourceCollection;
				messsageBuf.UnnededTopMessages++;
				if (CalcScrollPosHelper() > flatLogPosition)
					break;
			}
			FinalizeSourceBuffers ();
			if (AllLogsAreAtEnd())
				await MoveToStreamsEndInternal(cancellation);
			else
				SetScrolledLines (0);
		}

		private long CalcScrollPosHelper()
		{
			long currentPos = 0;
			foreach (var buf in buffers.Values)
				if (buf.UnnededTopMessages < buf.Count)
					currentPos += GetLineScrollPosition(buf.Source, buf.Get(buf.UnnededTopMessages));
				else
					currentPos += buf.Source.MapPositionToScrollPosition(buf.EndPosition);
			return currentPos;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferLines(
			SourceBuffer src,
			int count,
			CancellationToken cancellation)
		{
			var part1 = new ScreenBufferMessagesRange();
			part1.Lines = new List<DisplayLine>();
			if (src.Count > 0)
			{
				if (count < 0)
				{
					var top = src.Get(0);
					if (top.LineIndex != 0)
					{
						var messageLinesCount = top.Message.GetLinesCount();
						for (var i = 0; i < top.LineIndex; ++i)
						{
							part1.Lines.Add(new DisplayLine(top.Message, i, messageLinesCount));
							if (++count == 0)
								break;
						}
					}
				}
				else if (count > 0)
				{
					var botton = src.Get(src.Count - 1);
					var messageLinesCount = botton.Message.GetLinesCount();
					if (botton.LineIndex < (messageLinesCount - 1))
					{
						for (int i = botton.LineIndex + 1; i < messageLinesCount; ++i)
						{
							part1.Lines.Add(new DisplayLine(botton.Message, i, messageLinesCount));
							if (--count == 0)
								break;
						}
					}
				}
				if (part1.Lines.Count > 0)
				{
					part1.BeginPosition = part1.Lines[0].Message.Position;
					part1.EndPosition = part1.Lines.Last().Message.EndPosition;
				}
				if (count == 0)
				{
					return part1;
				}
			}
			var part2 = await GetScreenBufferLines(
				src.Source,
				count < 0 ? src.BeginPosition : src.EndPosition,
				Math.Abs(count),
				count < 0 ? EnumMessagesFlag.Backward : EnumMessagesFlag.Forward,
				cancellation
			);
			if (part1.Lines.Count == 0)
				return part2;
			if (count > 0)
			{
				part1.Lines.AddRange(part2.Lines);
				part1.EndPosition = part2.EndPosition;
			}
			else
			{
				part1.Lines.InsertRange(0, part2.Lines);
				part1.BeginPosition = part2.BeginPosition;
			}
			return part1;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferLines(
			IMessagesSource src, 
			long startFrom, 
			int maxCount, 
			EnumMessagesFlag flags, 
			CancellationToken cancellation)
		{
			var backward = (flags & EnumMessagesFlag.Backward) != 0 ;
			var lines = new List<DisplayLine>();
			await src.EnumMessages(startFrom, msg => 
			{
				var messagesLinesCount = msg.GetLinesCount();
				if (backward)
					for (int i = messagesLinesCount - 1; i >= 0; --i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount));
				else
					for (int i = 0; i < messagesLinesCount; ++i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount));
				return lines.Count < maxCount;
			}, flags | EnumMessagesFlag.IsActiveLogPositionHint, LogProviderCommandPriority.RealtimeUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			var firstRead = lines.FirstOrDefault();
			if (backward)
				lines.Reverse();
			var badPosition = backward ? src.PositionsRange.Begin : src.PositionsRange.End;
			ScreenBufferMessagesRange ret;
			ret.Lines = lines;
			ret.BeginPosition = lines.Count > 0 ? lines[0].Message.Position : badPosition;
			if (lines.Count == 0)
				ret.EndPosition = badPosition;
			else
				ret.EndPosition = backward ? startFrom : lines[lines.Count - 1].Message.EndPosition;
			return ret;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferLines(
			IMessagesSource src, DateTime dt, int maxCount, CancellationToken cancellation)
		{
			var startFrom = await src.GetDateBoundPosition(dt, ListUtils.ValueBound.Lower, 
				LogProviderCommandPriority.RealtimeUserAction, cancellation);
			cancellation.ThrowIfCancellationRequested();
			var lines = new List<DisplayLine>();
			var additionalMessagesCount = 0;
			await src.EnumMessages(
				startFrom.Position,
				msg => 
				{
					var messagesLinesCount = msg.GetLinesCount();
					for (int i = 0; i < messagesLinesCount; ++i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount));
					var pastRequestedTime = msg.Time.ToLocalDateTime() > dt;
					if (!pastRequestedTime)
						return true;
					++additionalMessagesCount;
					return additionalMessagesCount < maxCount;
				}, 
				EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, 
				LogProviderCommandPriority.RealtimeUserAction,
				cancellation
			);
			cancellation.ThrowIfCancellationRequested();
			var srcPositionsRange = src.PositionsRange;
			ScreenBufferMessagesRange ret;
			ret.Lines = lines;
			ret.BeginPosition = lines.Count > 0 ? lines[0].Message.Position : srcPositionsRange.End;
			if (lines.Count > 0)
				ret.EndPosition = lines[lines.Count - 1].Message.EndPosition;
			else
				ret.EndPosition = srcPositionsRange.End;
			return ret;
		}

		struct DisplayLine
		{
			public IMessage Message;
			public int LineIndex; // line number within the message
			public long LinePosition;
			public int Index;

			public DisplayLine(IMessage msg, int lineIndex, int linesCount)
			{
				Message = msg;
				LineIndex = lineIndex;
				if (linesCount > 1)
					LinePosition = msg.Position + (msg.EndPosition - msg.Position) * lineIndex / linesCount;
				else
					LinePosition = msg.Position;
				Index = -1;
			}

			public DisplayLine MakeIndexed(int index)
			{
				return new DisplayLine()
				{
					Message = Message,
					LineIndex = LineIndex,
					LinePosition = LinePosition,
					Index = index
				};
			}
		};

		struct ScreenBufferMessagesRange
		{
			public List<DisplayLine> Lines;
			public long BeginPosition, EndPosition;
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
				this.lines.AddRange(other.lines);
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

			// todo: hide public member
			public int UnnededTopMessages;

			public void Set(ScreenBufferMessagesRange range)
			{
				lines.Clear();
				lines.AddRange(range.Lines);
				beginPosition = range.BeginPosition;
				endPosition = range.EndPosition;
			}

			public void Append(ScreenBufferMessagesRange range)
			{
				Debug.Assert(endPosition == range.BeginPosition);
				lines.AddRange(range.Lines);
				endPosition = range.EndPosition;
			}

			public void Prepend(ScreenBufferMessagesRange range)
			{
				Debug.Assert(beginPosition == range.EndPosition);
				lines.InsertRange(0, range.Lines);
				beginPosition = range.BeginPosition;
			}

			public void Finalize(int maxSz)
			{
				if (UnnededTopMessages != 0)
				{
					if (lines.Count > UnnededTopMessages)
					{
						var newBeginMsg = lines[UnnededTopMessages];
						beginPosition = newBeginMsg.Message.Position;
					}
					else
					{
						beginPosition = endPosition;
					}
					lines.RemoveRange(0, UnnededTopMessages);
					UnnededTopMessages = 0;
				}
				if (lines.Count > maxSz)
				{
					lines.RemoveRange(maxSz, lines.Count - maxSz);
					if (lines.Count > 0)
						endPosition = lines[lines.Count - 1].Message.EndPosition;
				}
			}

			IEnumerable<IndexedMessage> IMessagesCollection.Forward (int begin, int end)
			{
				for (var i = begin; i != end; ++i)
					yield return new IndexedMessage(i, lines[i].Message);
			}

			IEnumerable<IndexedMessage> IMessagesCollection.Reverse (int begin, int end)
			{
				for (var i = begin; i != end; --i)
					yield return new IndexedMessage(i, lines[i].Message);
			}

			int IMessagesCollection.Count
			{
				get { return lines.Count; }
			}

			public int Count
			{
				get { return lines.Count; }
			}

			public DisplayLine Get(int idx)
			{
				return lines[idx];
			}

			readonly List<DisplayLine> lines = new List<DisplayLine>();
			readonly IMessagesSource source;
			long beginPosition;
			long endPosition;
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
				task = GetScreenBufferLines(x.Key, x.Key.PositionsRange.End, bufferSize, EnumMessagesFlag.Backward, cancellation)
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
				if (removingUnnededTopMessages)
				{
					sb.UnnededTopMessages++;
				}
				else if (viewSizeRemainder < 1)
				{
					SetScrolledLines(viewSizeRemainder);
					removingUnnededTopMessages = true;
				}
				else
				{
					viewSizeRemainder -= 1;
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
			var currentTop = EnumScreenBufferLines().FirstOrDefault();
			int nrOfLinesToLoad = 0;
			if (shiftByDisplayLines < 0)
			{
				nrOfLinesToLoad = -bufferSize;
				newScrolledLines -= shiftByDisplayLines;
			}
			else if (shiftByDisplayLines > 0)
			{
				nrOfLinesToLoad = shiftByDisplayLines;
				newScrolledLines -= shiftByDisplayLines;
			}
			else
			{
				SetScrolledLines(newScrolledLines);
				return 0;
			}

			Debug.Assert(Math.Abs(nrOfLinesToLoad) < bufferSize);
			Debug.Assert(currentTop.Message != null);

			var sourcesDict = buffers.Values.Select(s => new
			{
				buf = s,
				loadTask = GetScreenBufferLines(s, nrOfLinesToLoad, cancellation)
			}).ToList();
			await Task.WhenAll(sourcesDict.Select(t => t.loadTask));
			cancellation.ThrowIfCancellationRequested();

			// todo: do not change screen buffer until transaction is confirmed

			int loadedLines = 0;
			foreach (var src in sourcesDict)
			{
				var list = src.loadTask.Result;
				loadedLines += list.Lines.Count;
				if (nrOfLinesToLoad < 0)
					src.buf.Prepend(list);
				else
					src.buf.Append(list);
			}

			int newTopDisplayIndex =
				EnumScreenBufferLines()
				.Where(i => i.Message == currentTop.Message && i.LineIndex == currentTop.LineIndex)
				.Select(i => i.Index)
				.FirstOrDefault(-1);
			Debug.Assert(newTopDisplayIndex >= 0);

			int shiftedBy;

			if (nrOfLinesToLoad > 0)
			{
				foreach (var i in GetMessagesInternal().Forward(0, newTopDisplayIndex + nrOfLinesToLoad))
					((SourceBuffer)i.SourceCollection).UnnededTopMessages++;
				shiftedBy = nrOfLinesToLoad;
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
						shiftByDisplayLines += 1;
						shiftedBy -= 1;
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
			var sourceCollection = (SourceBuffer)m.SourceCollection;
			var line = sourceCollection.Get(m.SourceIndex);
			return new ScreenBufferMessage()
			{
				Message = line.Message,
				LineIndex = line.LineIndex,
				Index = m.Message.Index,
				Source = sourceCollection.Source
			};
		}

		Dictionary<IMessagesSource, SourceBuffer> buffers;
		double viewSize; // size of the view the screen buffer needs to fill. nr of lines.
		int bufferSize; // size of the buffer. it has enought messages to fill the view of size viewSize.
		double scrolledLines; // scrolling positon as nr of lines.
	};
};