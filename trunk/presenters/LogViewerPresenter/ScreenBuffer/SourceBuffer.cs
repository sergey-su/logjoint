using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	class SourceBuffer : IMessagesCollection
	{
		public SourceBuffer(IMessagesSource src, Diagnostics diagnostics, bool isRawLogMode)
		{
			this.source = src;
			this.diagnostics = diagnostics;
			this.isRawLogMode = isRawLogMode;
		}

		public SourceBuffer(SourceBuffer other)
		{
			this.source = other.source;
			this.beginPosition = other.beginPosition;
			this.endPosition = other.endPosition;
			this.isRawLogMode = other.isRawLogMode;
			this.lines.AddRange(other.lines);
			this.loggableId = source.LogSourceHint?.ConnectionId ?? this.GetHashCode().ToString("x8");
			this.diagnostics = other.diagnostics;
		}

		public IMessagesSource Source { get { return source; } }

		public string LoggableId { get { return loggableId; }}

		/// Position of the first message in the buffer 
		/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
		/// currently viewed time respectively.
		public long BeginPosition { get { return beginPosition; } }
		/// Position of the message following the last message in the buffer
		/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
		/// currently viewed time respectively.
		public long EndPosition { get { return endPosition; } }


		public void Reset(long position)
		{
			lines.Clear();
			beginPosition = position;
			endPosition = position;
		}

		public async Task LoadBefore(int nrOfLines, CancellationToken cancellation)
		{
			if (Count > 0)
			{
				var firstLine = Get(0);
				var existingNrOfLines = Math.Min(nrOfLines, firstLine.LineIndex);
				var range1 = new ScreenBufferLinesRange();
				range1.Lines = Enumerable.Range(firstLine.LineIndex - existingNrOfLines, existingNrOfLines)
					.Select(ln => new DisplayLine(firstLine.Message, ln, firstLine.TotalLinesInMessage, isRawLogMode, source)).ToList();
				range1.BeginPosition = firstLine.Message.Position;
				range1.EndPosition = BeginPosition; // todo
				Prepend(range1);
				nrOfLines -= existingNrOfLines;
			}
			if (nrOfLines > 0)
			{
				var range2 = await GetScreenBufferLines(Source, BeginPosition, nrOfLines,
					EnumMessagesFlag.Backward, isRawLogMode, diagnostics, cancellation);
				Prepend(range2);
			}
		}

		public async Task LoadAt(DateTime timestamp, int nrOfLines, CancellationToken cancellation)
		{
			var startFrom = await Source.GetDateBoundPosition(timestamp, ListUtils.ValueBound.Lower,
				LogProviderCommandPriority.RealtimeUserAction, cancellation);
			await LoadAround(startFrom.Position, nrOfLines, cancellation);
		}

		// todo: merge with LoadAt?
		public async Task LoadAt2(DateTime date, int nrOfLines, CancellationToken cancellation)
		{
			var r1 = await GetScreenBufferLines(source, date, nrOfLines, isRawLogMode, diagnostics, cancellation);
			var r2 = await GetScreenBufferLines(source, r1.BeginPosition, nrOfLines,
					EnumMessagesFlag.Backward, isRawLogMode, diagnostics, cancellation);
			Set(r1);
			Prepend(r2);
		}

		public async Task LoadAfter(int nrOfLines, CancellationToken cancellation)
		{
			if (Count > 0)
			{
				var lastLine = Get(Count - 1);
				var existingNrOfLines = Math.Min(nrOfLines, lastLine.TotalLinesInMessage - lastLine.LineIndex - 1);
				var range1 = new ScreenBufferLinesRange
				{
					Lines = Enumerable.Range(lastLine.LineIndex + 1, existingNrOfLines).Select(ln => new DisplayLine(
						lastLine.Message, ln, lastLine.TotalLinesInMessage, isRawLogMode, source)).ToList(),
					BeginPosition = EndPosition, // todo: use combined positioning streamPos+lineIdx
					EndPosition = lastLine.Message.EndPosition
				};
				Append(range1);
				nrOfLines -= existingNrOfLines;
			}
			if (nrOfLines > 0)
			{
				var range2 = await GetScreenBufferLines(Source, EndPosition, nrOfLines,
					EnumMessagesFlag.Forward, isRawLogMode, diagnostics, cancellation);
				Append(range2);
			}
		}

		public async Task LoadAround(long position, int nrOfLines,
			CancellationToken cancellation, bool doNotCountFirstMessage = false)
		{
			var range1 = await GetScreenBufferLines(source, position, nrOfLines,
				EnumMessagesFlag.Forward, isRawLogMode, diagnostics, cancellation, doNotCountFirstMessage);
			var range2 = await GetScreenBufferLines(source, range1.BeginPosition, nrOfLines,
				EnumMessagesFlag.Backward, isRawLogMode, diagnostics, cancellation, doNotCountFirstMessage);
			Set(range2);
			Append(range1);
		}

		public async Task LoadAround(int nrOfLines, CancellationToken cancellation)
		{
			await Task.WhenAll(new[]
			{
				LoadAfter(nrOfLines, cancellation),
				LoadBefore(nrOfLines, cancellation),
			});
			cancellation.ThrowIfCancellationRequested();
		}


		public void Cut(int unnededTopMessages, int maxSz)
		{
			if (unnededTopMessages != 0)
			{
				if (lines.Count > unnededTopMessages)
				{
					var newBeginMsg = lines[unnededTopMessages];
					beginPosition = newBeginMsg.Message.Position;
				}
				else
				{
					beginPosition = endPosition;
				}
				lines.RemoveRange(0, unnededTopMessages);
			}
			if (lines.Count > maxSz)
			{
				lines.RemoveRange(maxSz, lines.Count - maxSz);
				if (lines.Count > 0)
					endPosition = lines[lines.Count - 1].Message.EndPosition;
			}
			if (Debugger.IsAttached)
			{
				VerifyInvariants();
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

		public override string ToString()
		{
			return string.Format("[{0}, {1}), count={2}", beginPosition, endPosition, lines.Count);
		}

		private void VerifyInvariants()
		{
			diagnostics.VerifyLines(lines, source.HasConsecutiveMessages);
		}

		static async Task<ScreenBufferLinesRange> GetScreenBufferLines(
			IMessagesSource src,
			long startFrom,
			int maxCount,
			EnumMessagesFlag flags,
			bool rawLogMode,
			Diagnostics diag,
			CancellationToken cancellation,
			bool doNotCountFirstMessage = false // todo: needed?
		)
		{
			var backward = (flags & EnumMessagesFlag.Backward) != 0;
			var lines = new List<DisplayLine>();
			var loadedMessages = 0;
			var linesToIgnore = 0;
			await src.EnumMessages(startFrom, msg =>
			{
				var messagesLinesCount = msg.GetDisplayText(rawLogMode).GetLinesCount();
				if (backward)
					for (int i = messagesLinesCount - 1; i >= 0; --i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode, src));
				else
					for (int i = 0; i < messagesLinesCount; ++i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode, src));
				if (diag.IsEnabled)
					diag.VerifyLines(backward ? Enumerable.Reverse(lines) : lines, src.HasConsecutiveMessages);
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
			ScreenBufferLinesRange ret;
			ret.Lines = lines;
			ret.BeginPosition = lines.Count > 0 ? lines[0].Message.Position : badPosition;
			if (lines.Count == 0)
				ret.EndPosition = badPosition;
			else
				ret.EndPosition = lines[lines.Count - 1].Message.EndPosition;
			diag.VerifyLines(ret.Lines, src.HasConsecutiveMessages);
			return ret;
		}

		static async Task<ScreenBufferLinesRange> GetScreenBufferLines(
			IMessagesSource src,
			DateTime dt,
			int maxCount,
			bool rawLogMode,
			Diagnostics diag,
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
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode, src));
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
			ScreenBufferLinesRange ret;
			ret.Lines = lines;
			ret.BeginPosition = lines.Count > 0 ? lines[0].Message.Position : srcPositionsRange.End;
			if (lines.Count > 0)
				ret.EndPosition = lines[lines.Count - 1].Message.EndPosition;
			else
				ret.EndPosition = srcPositionsRange.End;
			diag.VerifyLines(ret.Lines, src.HasConsecutiveMessages);
			return ret;
		}

		private void Set(ScreenBufferLinesRange range)
		{
			lines.Clear();
			lines.AddRange(range.Lines);
			beginPosition = range.BeginPosition;
			endPosition = range.EndPosition;
			if (Debugger.IsAttached)
			{
				VerifyInvariants();
			}
		}

		private void Append(ScreenBufferLinesRange range)
		{
			Debug.Assert(endPosition == range.BeginPosition);
			lines.AddRange(range.Lines);
			endPosition = range.EndPosition;
			if (Debugger.IsAttached)
			{
				VerifyInvariants();
			}
		}

		private void Prepend(ScreenBufferLinesRange range)
		{
			Debug.Assert(beginPosition == range.EndPosition);
			lines.InsertRange(0, range.Lines);
			beginPosition = range.BeginPosition;
			if (Debugger.IsAttached)
			{
				VerifyInvariants();
			}
		}

		struct ScreenBufferLinesRange
		{
			public List<DisplayLine> Lines;
			public long BeginPosition, EndPosition;
		};

		readonly Diagnostics diagnostics;
		readonly string loggableId;
		readonly IMessagesSource source;
		readonly bool isRawLogMode;
		readonly List<DisplayLine> lines = new List<DisplayLine>();
		long beginPosition;
		long endPosition;
	};
};