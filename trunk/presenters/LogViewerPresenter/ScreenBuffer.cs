using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using LogJoint.MessagesContainers;

namespace LogJoint.UI.Presenters.LogViewer
{
	/// <summary>
	/// Maintains a buffer of log messages big enought to fill the view
	/// of given size.
	/// Interface consists of cancellable operations that modify the buffer asynchronously.
	/// Buffer stays consistent (usually unmodified) when an operation is cancelled.
	/// Only one operation at a time is possible. Before starting a new operation 
	/// previously started operations must complete or at least be cancelled.
	/// Threading: must be called from single thread assotiated with synchronization context 
	/// that posts completions to the same thread. UI thread meets these requirements.
	/// </summary>
	public interface IScreenBuffer
	{
		Task SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation);
		void SetViewSize(double sz);
		void SetRawLogMode(bool isRawMode);

		double ViewSize { get; }
		int FullyVisibleLinesCount { get; }
		IList<ScreenBufferEntry> Messages { get; }
		IEnumerable<SourceScreenBuffer> Sources { get; }
		double TopLineScrollValue { get; set; }
		double BufferPosition { get; }
		bool ContainsSource(IMessagesSource source);

		Task MoveToStreamsBegin(
			CancellationToken cancellation
		);
		Task MoveToStreamsEnd(
			CancellationToken cancellation
		);
		Task<bool> MoveToBookmark(
			IBookmark bookmark,
			BookmarkLookupMode mode,
			CancellationToken cancellation
		);
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
	};

	public interface IScreenBufferFactory
	{
		IScreenBuffer CreateScreenBuffer(InitialBufferPosition initialBufferPosition);
	};

	[Flags]
	public enum BookmarkLookupMode
	{
		MatchModeMask = 0xff,
		ExactMatch = 1,
		FindNearestBookmark = 2,
		FindNearestTime = 4,

		MoveBookmarkToMiddleOfScreen = 1024
	};

	/// <summary>
	/// Represents one line of log.
	/// It can be one log message or a part of multiline log message.
	/// </summary>
	public struct ScreenBufferEntry
	{
		/// <summary>
		/// Entry's index in ScreenBuffer's Messages collection
		/// </summary>
		public int Index;
		/// <summary>
		/// Reference to the message object. 
		/// Multiple entries can share refernce to same message but differ by TextLineIndex.
		/// </summary>
		public IMessage Message;
		/// <summary>
		/// Index of a line in Message's text.
		/// </summary>
		public int TextLineIndex;
		/// <summary>
		/// Source of the message.
		/// </summary>
		public IMessagesSource Source;
	};

	public struct SourceScreenBuffer
	{
		public IMessagesSource Source;
		public long Begin;
		public long End;
	};

	public enum InitialBufferPosition
	{
		StreamsBegin,
		StreamsEnd,
		Nowhere
	};

	public class ScreenBuffer: IScreenBuffer
	{
		internal ScreenBuffer(double viewSize, InitialBufferPosition initialBufferPosition, bool disableSingleLogPositioningOptimization = false)
		{
			this.buffers = new Dictionary<IMessagesSource, SourceBuffer>();
			this.entries = new List<ScreenBufferEntry>();
			this.entriesReadonly = entries.AsReadOnly();
			this.initialBufferPosition = initialBufferPosition;
			this.disableSingleLogPositioningOptimization = disableSingleLogPositioningOptimization;
			((IScreenBuffer)this).SetViewSize(viewSize);
		}

		double IScreenBuffer.ViewSize { get { return viewSize; } }

		int IScreenBuffer.FullyVisibleLinesCount { get { return (int) viewSize; } }

		async Task IScreenBuffer.SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("SetSources", cancellation))
			{
				var currentTop = EnumScreenBufferLines().FirstOrDefault();

				var newSources = sources.ToHashSet();
				int removed = 0;
				foreach (var s in buffers.Keys.ToArray())
					if (!newSources.Contains(s))
						removed += buffers.Remove(s) ? 1 : 0;
				newSources.RemoveWhere(s => buffers.ContainsKey(s));

				if (newSources.Count > 0)
				{
					foreach (var s in newSources)
						buffers.Add(s, new SourceBuffer(s));

					if (currentTop.Message != null)
					{
						var sourcesDict = newSources.Select(s => new
						{
							src = s,
							loadTask = GetScreenBufferLines(s, currentTop.Message.Time.ToLocalDateTime(),
								bufferSize, isRawLogMode, cancellation)
						}).ToList();
						await Task.WhenAll(sourcesDict.Select(t => t.loadTask));
						cancellation.ThrowIfCancellationRequested();

						foreach (var s in sourcesDict)
							buffers[s.src].Set(s.loadTask.Result);

						int newTopDisplayIndex =
							EnumScreenBufferLines()
								.Where(i => i.Message == currentTop.Message && i.LineIndex == currentTop.LineIndex)
								.Select(i => i.Index)
								.FirstOrDefault(-1);
						if (newTopDisplayIndex >= 0)
						{
							foreach (var i in MakeMergingCollection().Forward(0, newTopDisplayIndex))
								((SourceBuffer)i.SourceCollection).UnnededTopMessages++;
						}

						FinalizeSourceBuffers();
					}
					else
					{
						if (initialBufferPosition == InitialBufferPosition.StreamsEnd)
							await MoveToStreamsEndInternal(cancellation);
						else if (initialBufferPosition == InitialBufferPosition.StreamsBegin)
							await MoveToStreamsBeginInternal(cancellation);
					}
				}

				if (buffers.Count == 0)
				{
					SetScrolledLines(0);
				}

				if (removed > 0)
				{
					FinalizeSourceBuffers();
				}
			}
		}

		void IScreenBuffer.SetViewSize(double sz)
		{
			if (sz < 0)
				throw new ArgumentOutOfRangeException("view size");
			viewSize = sz;
			bufferSize = (int) Math.Ceiling(viewSize) + 1;
		}

		void IScreenBuffer.SetRawLogMode(bool isRawMode)
		{
			this.isRawLogMode = isRawMode;
		}

		double IScreenBuffer.TopLineScrollValue 
		{
			get { return scrolledLines; }
			set { SetScrolledLines(value); }
		}

		IList<ScreenBufferEntry> IScreenBuffer.Messages
		{
			get { return entriesReadonly; }
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

		bool IScreenBuffer.ContainsSource(IMessagesSource source)
		{
			return buffers.ContainsKey(source);
		}

		async Task IScreenBuffer.MoveToStreamsBegin(CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("MoveToStreamsBegin", cancellation))
			{
				await MoveToStreamsBeginInternal (cancellation);
			}
		}

		async Task IScreenBuffer.MoveToStreamsEnd(CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("MoveToStreamsEnd", cancellation))
			{
				await MoveToStreamsEndInternal(cancellation);
			}
		}

		async Task<bool> IScreenBuffer.MoveToBookmark(
			IBookmark bookmark,
			BookmarkLookupMode mode,
			CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation(string.Format("MoveToBookmark({0})", mode), cancellation))
			{
				var matchMode = mode & BookmarkLookupMode.MatchModeMask;
				MessageTimestamp dt = bookmark.Time;
				long position = bookmark.Position;
				int lineIndex = bookmark.LineIndex;
				string logSourceCollectionId = bookmark.LogSourceConnectionId;
				var tmp = buffers.ToDictionary(s => s.Key, s => new SourceBuffer(s.Value));
				var tasks = tmp.Select(s => new
				{
					buf = s.Value,
					task =
						(buffers.Count == 1 && matchMode == BookmarkLookupMode.ExactMatch) ? 
							GetScreenBufferLines(s.Key, position, bufferSize, EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, isRawLogMode, cancellation) :
							GetScreenBufferLines(s.Key, dt.ToLocalDateTime(), bufferSize, isRawLogMode, cancellation),
				}).ToList();
				await Task.WhenAll(tasks.Select(i => i.task));
				cancellation.ThrowIfCancellationRequested();
				foreach (var t in tasks)
					t.buf.Set(t.task.Result);
				bool messageFound = false;
				if (matchMode == BookmarkLookupMode.FindNearestTime)
				{
					messageFound = true;
				}
				else
				{
					foreach (var i in GetMessagesInternal(tmp.Values).Forward(0, int.MaxValue))
					{
						var cmp = MessagesComparer.CompareLogSourceConnectionIds(i.Message.Message.GetConnectionId(), logSourceCollectionId);
						if (cmp == 0)
							cmp = Math.Sign(i.Message.Message.Position - position);
						if (cmp == 0)
							cmp = Math.Sign(((SourceBuffer)i.SourceCollection).Get(i.SourceIndex).LineIndex - lineIndex);
						if (matchMode == BookmarkLookupMode.ExactMatch)
							messageFound = cmp == 0;
						else if (matchMode == BookmarkLookupMode.FindNearestBookmark)
							messageFound = cmp > 0;
						if (messageFound)
							break;
						var sb = ((SourceBuffer)i.SourceCollection);
						sb.UnnededTopMessages++;
					}
				}
				if (!messageFound)
				{
					if (matchMode == BookmarkLookupMode.FindNearestBookmark)
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
					if ((mode & BookmarkLookupMode.MoveBookmarkToMiddleOfScreen) != 0)
					{
						var additionalSpace = ((int)Math.Floor(viewSize) - 1) / 2;
						if (additionalSpace > 0)
							await ShiftByInternal(-additionalSpace, cancellation);
					}
				}

				return true;
			}
		}

		async Task<int> IScreenBuffer.ShiftBy(double nrOfDisplayLines, CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation(string.Format("ShiftBy({0})", nrOfDisplayLines), cancellation))
			{
				return await ShiftByInternal(nrOfDisplayLines, cancellation);
			}
		}

		async Task IScreenBuffer.Reload(CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("Reload", cancellation))
			{
				var tasks = buffers.Select(x => new
				{
					buf = x.Value,
					task = GetScreenBufferLines(x.Key, x.Value.BeginPosition, bufferSize,
						EnumMessagesFlag.Forward, isRawLogMode, cancellation)
				}).ToList();
				await Task.WhenAll(tasks.Select(x => x.task));
				cancellation.ThrowIfCancellationRequested();
				foreach (var t in tasks)
					t.buf.Set(t.task.Result);
				FinalizeSourceBuffers();
				if (AllLogsAreAtEnd())
					await MoveToStreamsEndInternal(cancellation);
			}
		}

		double IScreenBuffer.BufferPosition
		{
			get
			{
				long totalScrollLength = 0;
				foreach (var src in buffers.Values)
					totalScrollLength += src.Source.ScrollPositionsRange.Length;
				if (totalScrollLength == 0 || ViewIsTooSmall())
					return 0;
				foreach (var i in GetBufferZippedWithScrollPositions())
				{
					var lineScrollPosEnd = i.ScrollPositionEnd / (double)totalScrollLength;
					var lineViewPosEnd = ((double)i.Index + 1 - scrolledLines) / viewSize;
					if (lineViewPosEnd >= lineScrollPosEnd)
					{
						var lb = i.ScrollPositionBegin / (double)totalScrollLength;
						var le = lineScrollPosEnd;

						var vb = ((double)i.Index - scrolledLines) / viewSize;
						var ve = lineViewPosEnd;

						return vb + (lb - vb) * (ve - vb) / (ve - vb - le + lb);
					}
				}
				return 0;
			}
		}

		async Task IScreenBuffer.MoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			using (CreateTrackerForNewOperation(string.Format("MoveToPosition({0})", position), cancellation))
			{
				if (buffers.Count == 1 && !disableSingleLogPositioningOptimization)
					await MoveToPositionSingleLog(position, cancellation);
				else
					await MoveToPositionMultipleLogs(position, cancellation);
			}
		}

		public override string ToString()
		{
			var ret = new StringBuilder();
			foreach (var e in entries)
			{
				e.Message.GetDisplayText(isRawLogMode).GetNthTextLine(e.TextLineIndex).Append(ret);
				ret.AppendLine();
			}
			return ret.ToString();
		}

		IEnumerable<DisplayLine> EnumScreenBufferLines()
		{
			return MakeMergingCollection().Forward(0, int.MaxValue).Select(m => ((SourceBuffer)m.SourceCollection).Get(m.SourceIndex).MakeIndexed(m.Message.Index));
		}

		static MessagesContainers.MergingCollection GetMessagesInternal(IEnumerable<SourceBuffer> sourceBuffers)
		{
			return new MessagesContainers.SimpleMergingCollection(sourceBuffers);
		}

		MessagesContainers.MergingCollection MakeMergingCollection()
		{
			return GetMessagesInternal(buffers.Values);
		}

		IEnumerable<LineScrollInfo> GetBufferZippedWithScrollPositions()
		{
			foreach (var src in buffers.Values)
				src.CurrentIndex = 0;
			int bufferSize = 0;
			foreach (var m in MakeMergingCollection().Forward(0, int.MaxValue))
			{
				var source = (SourceBuffer)m.SourceCollection;
				var lineInfo = CalcScrollPosHelper();
				lineInfo.Index = m.Message.Index;
				lineInfo.Source = source;
				yield return lineInfo;
				source.CurrentIndex++;
				++bufferSize;
			}
			foreach (var src in buffers.Values)
				src.CurrentIndex = 0;
		}

		void FinalizeSourceBuffers()
		{
			foreach (var buf in buffers.Values)
			{
				buf.Finalize(bufferSize);
			}
			entries.Clear();
			entries.AddRange(MakeMergingCollection().Forward(0, bufferSize).Select(ToScreenBufferMessage));
		}

		async Task MoveToPositionSingleLog(
			double position,
			CancellationToken cancellation
		)
		{
			var buf = buffers.Values.Single();
			var scrollPosRange = buf.Source.ScrollPositionsRange;
			var posRange = buf.Source.PositionsRange;
			var scrollPosition = scrollPosRange.Begin + position * (double)scrollPosRange.Length;
			var range1 = await GetScreenBufferLines(
				buf.Source,
				buf.Source.MapScrollPositionToPosition((long)scrollPosition),
				bufferSize,
				EnumMessagesFlag.Backward,
				isRawLogMode,
				cancellation,
				doNotCountFirstMessage: true
			);
			var range2 = await GetScreenBufferLines(
				buf.Source,
				range1.EndPosition,
				bufferSize,
				EnumMessagesFlag.Forward,
				isRawLogMode,
				cancellation,
				doNotCountFirstMessage: true
			);
			buf.Set(range1);
			buf.Append(range2);

			MoveToPositionCore(position, scrollPosition);
			
			FinalizeSourceBuffers();
		}

		private void MoveToPositionCore(double position, double scrollPosition)
		{
			foreach (var i in GetBufferZippedWithScrollPositions())
			{
				if (i.ScrollPositionEnd >= scrollPosition)
				{
					double linePortion = Math.Max(0, (scrollPosition - i.ScrollPositionBegin) / (i.ScrollPositionEnd - i.ScrollPositionBegin));
					double targetViewPortion = position * viewSize - linePortion;
					int targetViewIndex = (int)Math.Ceiling(targetViewPortion);
					int idx = 0;
					foreach (var j in MakeMergingCollection().Reverse(i.Index, int.MinValue))
					{
						if (idx > targetViewIndex)
							((SourceBuffer)j.SourceCollection).UnnededTopMessages++;
						++idx;
					}
					SetScrolledLines(targetViewIndex - targetViewPortion);
					break;
				}
			}
		}

		async Task MoveToPositionMultipleLogs (
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
					datePosition += b.Key.MapPositionToScrollPosition(dateBound.Position);
				}
				return datePosition <= flatLogPosition;
			}) - 1;
			cancellation.ThrowIfCancellationRequested ();
			var date = fullDatesRange.Begin.AddMilliseconds (ms);
			var tasks = buffers.Select (s => new {
				buf = s.Value,
				task = GetScreenBufferLines(s.Key, date, bufferSize, isRawLogMode, cancellation)
			}).ToList ();
			await Task.WhenAll (tasks.Select (i => i.task));
			cancellation.ThrowIfCancellationRequested();
			var tasks2 = tasks.Select(s => new
			{
				buf = s.buf,
				task = GetScreenBufferLines(s.buf.Source, s.task.Result.BeginPosition, bufferSize,
					EnumMessagesFlag.Backward, isRawLogMode, cancellation)
			}).ToList();
			await Task.WhenAll(tasks2.Select(i => i.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var t in tasks)
				t.buf.Set (t.task.Result);
			foreach (var t in tasks2)
				t.buf.Prepend (t.task.Result);

			MoveToPositionCore(position, flatLogPosition);

			FinalizeSourceBuffers ();
		}

		private LineScrollInfo CalcScrollPosHelper()
		{
			var ret = new LineScrollInfo();

			foreach (var buf in buffers.Values)
			{
				if (buf.CurrentIndex < buf.Count)
				{
					var dl = buf.Get(buf.CurrentIndex);
					var msgBeginPos = buf.Source.MapPositionToScrollPosition(dl.Message.Position);
					ret.ScrollPositionBegin += msgBeginPos + dl.LineOffsetBegin;
					ret.ScrollPositionEnd += msgBeginPos + dl.LineOffsetEnd;
				}
				else
				{
					var bufEnd = buf.Source.MapPositionToScrollPosition(buf.EndPosition);
					ret.ScrollPositionBegin += bufEnd;
					ret.ScrollPositionEnd += bufEnd;
				}
			}
			
			return ret;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferLines(
			SourceBuffer src,
			int count,
			bool rawLogMode,
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
						var messageLinesCount = top.Message.GetDisplayText(rawLogMode).GetLinesCount();
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
					var messageLinesCount = botton.Message.GetDisplayText(rawLogMode).GetLinesCount();
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
				rawLogMode,
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
			bool rawLogMode,
			CancellationToken cancellation,
			bool doNotCountFirstMessage = false
		)
		{
			var backward = (flags & EnumMessagesFlag.Backward) != 0 ;
			var lines = new List<DisplayLine>();
			var loadedMessages = 0;
			var linesToIgnore = 0;
			await src.EnumMessages(startFrom, msg => 
			{
				var messagesLinesCount = msg.GetDisplayText(rawLogMode).GetLinesCount();
				if (backward)
					for (int i = messagesLinesCount - 1; i >= 0; --i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount));
				else
					for (int i = 0; i < messagesLinesCount; ++i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount));
				++loadedMessages;
				if (doNotCountFirstMessage && loadedMessages == 1)
					linesToIgnore = lines.Count;
				return (lines.Count - linesToIgnore) < maxCount;
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
				ret.EndPosition = lines[lines.Count - 1].Message.EndPosition;
			return ret;
		}

		static async Task<ScreenBufferMessagesRange> GetScreenBufferLines(
			IMessagesSource src,
			DateTime dt,
			int maxCount,
			bool rawLogMode,
			CancellationToken cancellation)
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
					var messagesLinesCount = msg.GetDisplayText(rawLogMode).GetLinesCount();
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

		OperationTracker CreateTrackerForNewOperation(string operationName, CancellationToken operationCancellation)
		{
			if (currentOperationTracker != null && !currentOperationTracker.cancellation.IsCancellationRequested)
			{
				throw new InvalidOperationException(
					string.Format("Impossible to start new operation '{0}' while previous one '{1}' is not finished or cancelled",
						operationName, currentOperationTracker.name));
			}
			currentOperationTracker = new OperationTracker(this, operationName, operationCancellation);
			return currentOperationTracker;
		}

		struct LineScrollInfo
		{
			public int Index;
			public double ScrollPositionBegin, ScrollPositionEnd;
			public SourceBuffer Source;
		};

		struct DisplayLine
		{
			public IMessage Message;
			public int LineIndex; // line number within the message
			public double LineOffsetBegin, LineOffsetEnd;
			public int Index;

			public DisplayLine(IMessage msg, int lineIndex, int linesCount)
			{
				Message = msg;
				LineIndex = lineIndex;
				var msgLen = msg.EndPosition - msg.Position;
				if (linesCount > 1)
				{
					var lineLen = msgLen / (double)linesCount;
					LineOffsetBegin = lineLen * lineIndex;
					LineOffsetEnd = (lineIndex + 1) * lineLen;
					if (lineIndex == linesCount - 1)
					{
						// this it to ensure the offset is strickly equal to the beginning of next message.
						// generic formula with floating point arithmetics leads to inequality.
						LineOffsetEnd = msgLen;
					}
				}
				else
				{
					LineOffsetBegin = 0;
					LineOffsetEnd = msgLen;
				}
				Index = -1;
			}

			public DisplayLine MakeIndexed(int index)
			{
				return new DisplayLine()
				{
					Message = Message,
					LineIndex = LineIndex,
					LineOffsetBegin = LineOffsetBegin,
					LineOffsetEnd = LineOffsetEnd,
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

			public int UnnededTopMessages;
			public int CurrentIndex;

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
				CurrentIndex = 0;
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

			public override string ToString()
			{
				return string.Format("[{0}, {1}), count={2}", beginPosition, endPosition, lines.Count);
			}

			readonly List<DisplayLine> lines = new List<DisplayLine>();
			readonly IMessagesSource source;
			long beginPosition;
			long endPosition;
		};

		void SetScrolledLines(double value)
		{
			if (value < 0 || value >= 1d)
				throw new ArgumentOutOfRangeException();
			scrolledLines = value;
		}

		bool AllLogsAreAtEnd()
		{
			return 
				buffers.All(b => b.Value.EndPosition == b.Key.PositionsRange.End) 
			 && ((IMessagesCollection)MakeMergingCollection()).Count <= viewSize + scrolledLines;
		}

		async Task MoveToStreamsBeginInternal (CancellationToken cancellation)
		{
			var tasks = buffers.Select (x => new {
				buf = x.Value,
				task = GetScreenBufferLines (x.Key, x.Key.PositionsRange.Begin, bufferSize, EnumMessagesFlag.Forward, isRawLogMode, cancellation)
			}).ToList ();
			await Task.WhenAll (tasks.Select (x => x.task));
			cancellation.ThrowIfCancellationRequested ();
			foreach (var x in tasks)
				x.buf.Set (x.task.Result);
			FinalizeSourceBuffers ();
			SetScrolledLines (0);
		}

		async Task MoveToStreamsEndInternal(CancellationToken cancellation)
		{
			var tasks = buffers.Select(x => new
			{
				buf = x.Value,
				task = GetScreenBufferLines(x.Key, x.Key.PositionsRange.End, bufferSize,
					EnumMessagesFlag.Backward, isRawLogMode, cancellation)
			}).ToList();
			await Task.WhenAll(tasks.Select(x => x.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var x in tasks)
				x.buf.Set(x.task.Result);
			double viewSizeRemainder = viewSize;
			bool removingUnnededTopMessages = false;
			foreach (var x in MakeMergingCollection().Reverse(int.MaxValue, int.MinValue))
			{
				var sb = (SourceBuffer)x.SourceCollection;
				if (removingUnnededTopMessages)
				{
					sb.UnnededTopMessages++;
				}
				else if (viewSizeRemainder < 1)
				{
					if (viewSizeRemainder == 0)
						SetScrolledLines(0);
					else
						SetScrolledLines(1 - viewSizeRemainder);
					removingUnnededTopMessages = true;
				}
				else
				{
					viewSizeRemainder -= 1;
					if (viewSizeRemainder == 0)
					{
						sb.UnnededTopMessages++;
					}
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
				if (!AllLogsAreAtEnd())
					SetScrolledLines(newScrolledLines);
				return 0;
			}

			Debug.Assert(Math.Abs(nrOfLinesToLoad) < bufferSize);
			Debug.Assert(currentTop.Message != null);

			var sourcesDict = buffers.Values.Select(s => new
			{
				buf = s,
				loadTask = GetScreenBufferLines(s, nrOfLinesToLoad, isRawLogMode, cancellation)
			}).ToList();
			await Task.WhenAll(sourcesDict.Select(t => t.loadTask));
			cancellation.ThrowIfCancellationRequested();

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
				foreach (var i in MakeMergingCollection().Forward(0, newTopDisplayIndex + nrOfLinesToLoad))
					((SourceBuffer)i.SourceCollection).UnnededTopMessages++;
				shiftedBy = loadedLines;
			}
			else
			{
				shiftedBy = 0;
				bool markingUnneeded = false;
				foreach (var i in MakeMergingCollection().Reverse(newTopDisplayIndex - 1, int.MinValue))
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

		static ScreenBufferEntry ToScreenBufferMessage(MessagesContainers.MergingCollectionEntry m)
		{
			var sourceCollection = (SourceBuffer)m.SourceCollection;
			var line = sourceCollection.Get(m.SourceIndex);
			return new ScreenBufferEntry()
			{
				Message = line.Message,
				TextLineIndex = line.LineIndex,
				Index = m.Message.Index,
				Source = sourceCollection.Source
			};
		}

		bool ViewIsTooSmall()
		{
			return viewSize < 1e-2;
		}

		class OperationTracker: IDisposable
		{
			public readonly ScreenBuffer owner;
			public readonly string name;
			public readonly CancellationToken cancellation;

			public OperationTracker(ScreenBuffer owner, string name, CancellationToken cancellation)
			{
				this.owner = owner;
				this.name = name;
				this.cancellation = cancellation;
			}

			public void Dispose()
			{
				if (owner.currentOperationTracker == this)
				{
					owner.currentOperationTracker = null;
				}
			}
		};

		Dictionary<IMessagesSource, SourceBuffer> buffers;
		List<ScreenBufferEntry> entries;
		IList<ScreenBufferEntry> entriesReadonly;
		readonly InitialBufferPosition initialBufferPosition;
		readonly bool disableSingleLogPositioningOptimization;
		double viewSize; // size of the view the screen buffer needs to fill. nr of lines.
		int bufferSize; // size of the buffer. it has enought messages to fill the view of size viewSize.
		double scrolledLines; // scrolling positon as nr of lines. [0..1)
		bool isRawLogMode;
		OperationTracker currentOperationTracker;
	};

	public class ScreenBufferFactory : IScreenBufferFactory
	{
		IScreenBuffer IScreenBufferFactory.CreateScreenBuffer(InitialBufferPosition initialBufferPosition)
		{
			return new ScreenBuffer(0, initialBufferPosition);
		}
	};

	public static class Extenstions
	{
		public static ViewLine ToViewLine(this ScreenBufferEntry e)
		{
			return new ViewLine()
			{
				Message = e.Message,
				LineIndex = e.Index,
				TextLineIndex = e.TextLineIndex,
			};
		}
	};
};