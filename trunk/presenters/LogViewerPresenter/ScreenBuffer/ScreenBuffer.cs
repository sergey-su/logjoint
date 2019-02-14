using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class ScreenBuffer: IScreenBuffer
	{
		internal ScreenBuffer(
			double viewSize, 
			LJTraceSource trace = null,
			bool disableSingleLogPositioningOptimization = false
		)
		{
			this.buffers = new Dictionary<IMessagesSource, SourceBuffer>();
			this.entries = new List<ScreenBufferEntry>();
			this.entriesReadonly = entries.AsReadOnly();
			this.disableSingleLogPositioningOptimization = disableSingleLogPositioningOptimization;
			this.trace = trace ?? LJTraceSource.EmptyTracer;
			this.SetViewSize(viewSize);
		}

		double IScreenBuffer.ViewSize { get { return viewSize; } }

		int IScreenBuffer.FullyVisibleLinesCount { get { return (int) viewSize; } }

		async Task IScreenBuffer.SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("SetSources", cancellation))
			{
				var currentTop = EnumScreenBufferLines().FirstOrDefault();

				var oldBuffers = buffers;
				buffers = sources.ToDictionary(s => s, s => oldBuffers.ContainsKey(s) ? oldBuffers[s] : new SourceBuffer(s, diagnostics));
				bool needsClean = false;

				if (currentTop.Message != null)
				{
					var currentTopSourcePresent = buffers.ContainsKey(currentTop.Source.Source);

					await Task.WhenAll(buffers.Select(s => oldBuffers.ContainsKey(s.Key) ?
						LoadAround(s.Value, GetMaxBufferSize(viewSize), isRawLogMode, diagnostics, cancellation) :
						LoadAt(s.Value, currentTop.Message.Time.ToLocalDateTime(), GetMaxBufferSize(viewSize), isRawLogMode, diagnostics, cancellation)
					));
					cancellation.ThrowIfCancellationRequested();

					if (currentTopSourcePresent)
						needsClean = FinalizeBuffers(buffers, l =>
						{
							if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex)
								return -scrolledLines;
							return null;
						}) == null;
					else
						needsClean = FinalizeBuffers2(buffers, lines =>
						{
							var best = lines
								.Select(l => new { l, diff = (l.Message.Time.ToLocalDateTime() - currentTop.Message.Time.ToLocalDateTime()).Abs()})
								.Aggregate(new { l = new DisplayLine(), diff = TimeSpan.MaxValue }, (acc, l) => l.diff < acc.diff ? l : acc);
							return best.l.Message != null ? new KeyValuePair<DisplayLine, double>(best.l, 0) : new KeyValuePair<DisplayLine, double>?();
						}) == null;
				}
				else
				{
					needsClean = FinalizeBuffers(buffers, l =>
					{
						return 0;
					}) == null;
				}
				if (needsClean)
				{
					Clean();
				}
			}
		}

		private static async Task LoadBefore(SourceBuffer buf, int nrOfLines, bool isRawLogMode,
			Diagnostics diag, CancellationToken cancellation)
		{
			// todo: do not modify if cancelled
			if (buf.Count > 0)
			{
				var firstLine = buf.Get(0);
				if (firstLine.LineIndex > 0)  // todo: unneeded condition
				{
					var existingNrOfLines = Math.Min(nrOfLines, firstLine.LineIndex);
					var range1 = new ScreenBufferLinesRange();
					range1.Lines = Enumerable.Range(firstLine.LineIndex - existingNrOfLines, existingNrOfLines).Select(ln => new DisplayLine(firstLine.Message, ln, firstLine.TotalLinesInMessage, isRawLogMode)).ToList();
					range1.BeginPosition = firstLine.Message.Position;
					range1.EndPosition = buf.BeginPosition; // todo
					buf.Prepend(range1);
					nrOfLines -= existingNrOfLines;
				}
			}
			if (nrOfLines > 0)
			{
				var range2 = await GetScreenBufferLines(buf.Source, buf.BeginPosition, nrOfLines,
					EnumMessagesFlag.Backward, isRawLogMode, diag, cancellation);
				buf.Prepend(range2);
			}
		}

		private static async Task LoadAt(SourceBuffer buf, DateTime timestamp, int nrOfLines, bool isRawLogMode,
			Diagnostics diag, CancellationToken cancellation)
		{
			var startFrom = await buf.Source.GetDateBoundPosition(timestamp, ListUtils.ValueBound.Lower,
				LogProviderCommandPriority.RealtimeUserAction, cancellation);
			await LoadAround(buf, startFrom.Position, nrOfLines, isRawLogMode, diag, cancellation);
		}

		private static async Task LoadAfter(SourceBuffer buf, int nrOfLines, bool isRawLogMode,
			Diagnostics diag, CancellationToken cancellation)
		{
			// todo: do not modify if cancelled
			if (buf.Count > 0)
			{
				var lastLine = buf.Get(buf.Count - 1);
				if (lastLine.LineIndex < lastLine.TotalLinesInMessage - 1) // todo: unneeded
				{
					var existingNrOfLines = Math.Min(nrOfLines, lastLine.TotalLinesInMessage - lastLine.LineIndex - 1);
					var range1 = new ScreenBufferLinesRange();
					range1.Lines = Enumerable.Range(lastLine.LineIndex + 1, existingNrOfLines).Select(ln => new DisplayLine(lastLine.Message, ln, lastLine.TotalLinesInMessage, isRawLogMode)).ToList();
					range1.BeginPosition = buf.EndPosition; // todo: use combined positioning streamPos+lineIdx
					range1.EndPosition = lastLine.Message.EndPosition;
					buf.Append(range1);
					nrOfLines -= existingNrOfLines;
				}
			}
			if (nrOfLines > 0)
			{
				var range2 = await GetScreenBufferLines(buf.Source, buf.EndPosition, nrOfLines,
					EnumMessagesFlag.Forward, isRawLogMode, diag, cancellation);
				buf.Append(range2);
			}
		}

		private static async Task LoadAround(SourceBuffer buf, long position, int nrOfLines, bool isRawLogMode,
			Diagnostics diag, CancellationToken cancellation)
		{
			var range1 = await GetScreenBufferLines(buf.Source, position, nrOfLines,
				EnumMessagesFlag.Forward, isRawLogMode, diag, cancellation);
			var range2 = await GetScreenBufferLines(buf.Source, 
				range1.Lines.Count > 0 ? range1.Lines.First().Message.Position : position, nrOfLines,
				EnumMessagesFlag.Backward, isRawLogMode, diag, cancellation);
			buf.Set(range2);
			buf.Append(range1);
		}

		private static async Task LoadAround(SourceBuffer buf, int nrOfLines, bool isRawLogMode,
			Diagnostics diag, CancellationToken cancellation)
		{
			await Task.WhenAll(new[]
			{
				LoadAfter(buf, nrOfLines, isRawLogMode, diag, cancellation),
				LoadBefore(buf, nrOfLines, isRawLogMode, diag, cancellation),
			});
			cancellation.ThrowIfCancellationRequested();
		}

		async Task IScreenBuffer.SetViewSize(double sz, CancellationToken cancellation)
		{
			var currentTop = EnumScreenBufferLines().FirstOrDefault();

			var tmp = Clone();

			// todo: load beginnings only if there is not enough lines
			await Task.WhenAll(tmp.Select(x => LoadAround(x.Value, GetMaxBufferSize(sz), isRawLogMode, diagnostics, cancellation)));
			cancellation.ThrowIfCancellationRequested();

			SetViewSize(sz);

			FinalizeBuffers(tmp, l =>
			{
				if (currentTop.Message == null)
				{
					return 0;
				}
				if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex)
				{
					return -scrolledLines;
				}
				return null;
			});
		}

		void IScreenBuffer.SetRawLogMode(bool isRawMode)
		{
			this.isRawLogMode = isRawMode;
			// todo: reload?
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

		async Task IScreenBuffer.MoveToStreamsBegin(CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("MoveToStreamsBegin", cancellation))
			{
				var tmp = Clone();
				var tasks = tmp.Select(x => new {
					buf = x.Value,
					task = GetScreenBufferLines(x.Key, x.Key.PositionsRange.Begin, bufferSize, EnumMessagesFlag.Forward, isRawLogMode, diagnostics, cancellation)
				}).ToList();
				await Task.WhenAll(tasks.Select(x => x.task));
				cancellation.ThrowIfCancellationRequested();
				foreach (var x in tasks)
					x.buf.Set(x.task.Result);
				FinalizeBuffers(tmp, l => 0);
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

				var tmp = Clone();
				await Task.WhenAll(tmp.Select(buf =>
						matchMode == BookmarkLookupMode.ExactMatch && buf.Key.LogSourceHint?.ConnectionId == bookmark.LogSourceConnectionId ?
					LoadAround(buf.Value, bookmark.Position, GetMaxBufferSize(viewSize) + bookmark.LineIndex, isRawLogMode, diagnostics, cancellation) :
					LoadAt(buf.Value, bookmark.Time.ToLocalDateTime(), GetMaxBufferSize(viewSize) + bookmark.LineIndex, isRawLogMode, diagnostics, cancellation)
				));
				cancellation.ThrowIfCancellationRequested();

				double matchedMessagePosition = (mode & BookmarkLookupMode.MoveBookmarkToMiddleOfScreen) != 0 ? Math.Max(viewSize - 1d, 0) / 2d : 0d;

				Func<DisplayLine, int> cmp = (DisplayLine l) =>
				{
					var ret = MessagesComparer.CompareLogSourceConnectionIds(l.Message.GetConnectionId(), bookmark.LogSourceConnectionId);
					if (ret == 0)
						ret = Math.Sign(l.Message.Position - bookmark.Position);
					if (ret == 0)
						ret = Math.Sign(l.LineIndex - bookmark.LineIndex);
					return ret;
				};

				return FinalizeBuffers2(tmp, lines =>
				{
					var ret = lines.FirstOrDefault(l =>
					{
						if (matchMode == BookmarkLookupMode.ExactMatch)
							return cmp(l) == 0;
						else if (matchMode == BookmarkLookupMode.FindNearestMessage)
							return cmp(l) >= 0;
						else
							return l.Message.Time >= bookmark.Time;
					});
					return ret.Message == null ? new KeyValuePair<DisplayLine, double>?() :
						new KeyValuePair<DisplayLine, double>(ret, matchedMessagePosition);
				}) != null;

				/*MessageTimestamp dt = bookmark.Time;
				long position = bookmark.Position;
				int lineIndex = bookmark.LineIndex;
				string logSourceCollectionId = bookmark.LogSourceConnectionId;
				var tmp = buffers.ToDictionary(s => s.Key, s => new SourceBuffer(s.Value));
				var tasks = tmp.Select(s => new
				{
					buf = s.Value,
					task =
						(buffers.Count == 1 && matchMode == BookmarkLookupMode.ExactMatch) ? 
							GetScreenBufferLines(s.Key, position, bufferSize + lineIndex, EnumMessagesFlag.Forward | EnumMessagesFlag.IsActiveLogPositionHint, isRawLogMode, cancellation) :
							GetScreenBufferLines(s.Key, dt.ToLocalDateTime(), bufferSize + lineIndex, isRawLogMode, cancellation),
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

				return true;*/

			}
		}

		async Task<ScreenBufferEntry?> IScreenBuffer.MoveToTimestamp(
			DateTime timestamp,
			CancellationToken cancellation
		)
		{
			using (CreateTrackerForNewOperation(string.Format("MoveToTimestamp({0})", timestamp.ToString("O")), cancellation))
			{
				var tmp = Clone();
				await Task.WhenAll(tmp.Select(buf =>
					LoadAt(buf.Value, timestamp, GetMaxBufferSize(viewSize), isRawLogMode, diagnostics, cancellation)
				));
				cancellation.ThrowIfCancellationRequested();

				double matchedMessagePosition = Math.Max(viewSize - 1d, 0) / 2d; // todo: share with move to bmk

				Func<IEnumerable<DisplayLine>, DisplayLine> findNearest = (lines) =>
				{
					return lines.FirstOrDefault(l => l.Message.Time.ToLocalDateTime() >= timestamp); // todo: look for nearest
				};

				if (FinalizeBuffers2(tmp, lines =>
				{
					var ret = findNearest(lines);
					return ret.Message == null ? new KeyValuePair<DisplayLine, double>?() :
						new KeyValuePair<DisplayLine, double>(ret, matchedMessagePosition);
				}) == null)
				{
					return null;
				}

				var line = findNearest(EnumScreenBufferLines());
				if (line.Message == null)
					return null;

				return new ScreenBufferEntry()
				{
					Index = line.Index,
					Message = line.Message,
					Source = line.Source.Source,
					TextLineIndex = line.LineIndex
				};
			}
		}

		async Task<double> IScreenBuffer.ShiftBy(double nrOfDisplayLines, CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation(string.Format("ShiftBy({0})", nrOfDisplayLines), cancellation))
			{
				var currentTop = EnumScreenBufferLines().FirstOrDefault();
				if (currentTop.Message == null)
					return 0;

				var tmp = Clone();
				var saveScrolledLines = scrolledLines;

				await Task.WhenAll(tmp.Select(buf =>
					LoadAround(buf.Value, GetMaxBufferSize(viewSize + Math.Abs(nrOfDisplayLines)), isRawLogMode, diagnostics, cancellation)
				));
				cancellation.ThrowIfCancellationRequested();

				var pivotLinePosition = FinalizeBuffers(tmp, l =>
				{
					if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex)
						return -nrOfDisplayLines - scrolledLines;
					return null;
				});
				if (!pivotLinePosition.HasValue)
					return 0;

				return -saveScrolledLines - pivotLinePosition.Value;
			}
		}

		async Task IScreenBuffer.Reload(CancellationToken cancellation)
		{
			using (CreateTrackerForNewOperation("Reload", cancellation))
			{
				// var topLine = EnumScreenBufferLines().FirstOrDefault();
				var tasks = buffers.Select(x => new
				{
					buf = x.Value,
					task = GetScreenBufferLines(x.Key, x.Value.BeginPosition, GetMaxBufferSize(viewSize),
						EnumMessagesFlag.Forward, isRawLogMode, diagnostics, cancellation)
				}).ToList();
				await Task.WhenAll(tasks.Select(x => x.task));
				cancellation.ThrowIfCancellationRequested();
				//FinalizeBuffers(l => MessagesComparer.Compare(l.Message, topLine.Message) == 0 && l.LineIndex == topLine.LineIndex ? this.scrolledLines : new double?());
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

		static IEnumerable<DisplayLine> EnumScreenBufferLines(IEnumerable<IMessagesCollection> colls)
		{
			return MakeMergingCollection(colls).Forward(0, int.MaxValue).Select(m => ((SourceBuffer)m.SourceCollection).Get(m.SourceIndex).MakeIndexed(m.Message.Index, (SourceBuffer)m.SourceCollection));
		}

		IEnumerable<DisplayLine> EnumScreenBufferLines()
		{
			return EnumScreenBufferLines(buffers.Values);
		}

		static MessagesContainers.MergingCollection MakeMergingCollection(IEnumerable<IMessagesCollection> colls)
		{
			return new MessagesContainers.SimpleMergingCollection(colls);
		}

		MessagesContainers.MergingCollection MakeMergingCollection()
		{
			return MakeMergingCollection(buffers.Values);
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
				diagnostics,
				cancellation,
				doNotCountFirstMessage: true
			);
			var range2 = await GetScreenBufferLines(
				buf.Source,
				range1.EndPosition,
				bufferSize,
				EnumMessagesFlag.Forward,
				isRawLogMode,
				diagnostics,
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
				task = GetScreenBufferLines(s.Key, date, bufferSize, isRawLogMode, diagnostics, cancellation)
			}).ToList ();
			await Task.WhenAll (tasks.Select (i => i.task));
			cancellation.ThrowIfCancellationRequested();
			var tasks2 = tasks.Select(s => new
			{
				buf = s.buf,
				task = GetScreenBufferLines(s.buf.Source, s.task.Result.BeginPosition, bufferSize,
					EnumMessagesFlag.Backward, isRawLogMode, diagnostics, cancellation)
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

		static async Task<ScreenBufferLinesRange> GetScreenBufferLines(
			SourceBuffer src,
			int count,
			bool rawLogMode,
			Diagnostics diag,
			CancellationToken cancellation)
		{
			var part1 = new ScreenBufferLinesRange();
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
							part1.Lines.Add(new DisplayLine(top.Message, i, messageLinesCount, rawLogMode));
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
							part1.Lines.Add(new DisplayLine(botton.Message, i, messageLinesCount, rawLogMode));
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
				diag,
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

		static async Task<ScreenBufferLinesRange> GetScreenBufferLines(
			IMessagesSource src, 
			long startFrom, 
			int maxCount, 
			EnumMessagesFlag flags, 
			bool rawLogMode,
			Diagnostics diag,
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
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode));
				else
					for (int i = 0; i < messagesLinesCount; ++i)
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode));
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
						lines.Add(new DisplayLine(msg, i, messagesLinesCount, rawLogMode));
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

		private void SetViewSize(double sz)
		{
			if (sz < 0)
				throw new ArgumentOutOfRangeException("view size");
			viewSize = sz;
			bufferSize = (int)Math.Ceiling(viewSize) + 1;
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

		Dictionary<IMessagesSource, SourceBuffer> Clone()
		{
			return buffers.ToDictionary(s => s.Key, s => new SourceBuffer(s.Value));
		}

		async Task MoveToStreamsEndInternal(CancellationToken cancellation)
		{
			var tmp = Clone();
			var tasks = tmp.Select(x => new
			{
				buf = x.Value,
				task = GetScreenBufferLines(x.Key, x.Key.PositionsRange.End, bufferSize,
					EnumMessagesFlag.Backward, isRawLogMode, diagnostics, cancellation)
			}).ToList();
			await Task.WhenAll(tasks.Select(x => x.task));
			cancellation.ThrowIfCancellationRequested();
			foreach (var x in tasks)
				x.buf.Set(x.task.Result);
			FinalizeBuffers2(tmp, lines =>
			{
				return lines.Count > 0 ? new KeyValuePair<DisplayLine, double>(lines.Last(), viewSize - 1) : new KeyValuePair<DisplayLine, double>?();
			});
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

		double? FinalizeBuffers(Dictionary<IMessagesSource, SourceBuffer> tmp, Func<DisplayLine, double?> testPivotLine)
		{
			return FinalizeBuffers2(tmp, lines =>
			{
				foreach (var l in lines)
				{
					var testResult = testPivotLine(l);
					if (testResult != null)
						return new KeyValuePair<DisplayLine, double>(l, testResult.Value);
				}
				return null;
			});
		}

		double? FinalizeBuffers2(Dictionary<IMessagesSource, SourceBuffer> tmp, Func<List<DisplayLine>, KeyValuePair<DisplayLine, double>?> getPivotLine) // todo: better name
		{
			var lines = EnumScreenBufferLines(tmp.Values).ToList();
			var pivotLine = getPivotLine(lines);
			foreach (var line in lines)
			{
				if (pivotLine.HasValue && line.Index == pivotLine.Value.Key.Index)
				{
					double idx = pivotLine.Value.Value;
					int idxWhole = (int)Math.Ceiling(idx);

					int topLineIdx = line.Index - idxWhole;
					double topLineScroll = idxWhole - idx;
					double ret = idx;

					Action<int, double> applyContraint = (int newTopLineIdx, double newTopLineScroll) =>
					{
						ret += ((double)topLineIdx - topLineScroll) - ((double)newTopLineIdx - newTopLineScroll);
						topLineIdx = newTopLineIdx;
						topLineScroll = newTopLineScroll;
					};

					var bufferSize = (int)Math.Ceiling(viewSize + topLineScroll);

					if (topLineIdx + topLineScroll + viewSize > lines.Count)
					{
						applyContraint(lines.Count - (int)Math.Ceiling(viewSize), Math.Ceiling(viewSize) - viewSize);
					}
					if (topLineIdx < 0)
					{
						applyContraint(0, 0);
					}

					foreach (var l in lines)
					{
						if (l.Index >= topLineIdx)
							break;
						else
							l.Source.UnnededTopMessages++;
					}

					foreach (var buf in tmp.Values)
					{
						buf.Finalize(bufferSize);
					}

					buffers = tmp;
					entries.Clear();
					entries.AddRange(MakeMergingCollection().Forward(0, bufferSize).Select(ToScreenBufferMessage));

					SetScrolledLines(topLineScroll);

					if (Debugger.IsAttached)
					{
						VerifyInvariants();
					}

					return ret;
				}
			}
			
			return null;
		}

		void VerifyInvariants()
		{
			diagnostics.VerifyLines(entries, (buffers.FirstOrDefault().Key?.HasConsecutiveMessages).GetValueOrDefault(false));
		}

		void Clean()
		{
			entries.Clear();
			SetScrolledLines(0);
		}

		class OperationTracker: IDisposable
		{
			public readonly ScreenBuffer owner;
			public readonly string name;
			public readonly CancellationToken cancellation;
			public readonly Profiling.Operation perfop;

			public OperationTracker(ScreenBuffer owner, string name, CancellationToken cancellation)
			{
				this.owner = owner;
				this.name = name;
				this.cancellation = cancellation;
				this.perfop = owner.CreatePerfop(name);
			}

			public void Dispose()
			{
				perfop.Dispose();
				if (owner.currentOperationTracker == this)
				{
					owner.currentOperationTracker = null;
				}
			}
		};

		Profiling.Operation CreatePerfop(string name)
		{
			if (profilingEnabled)
				return new Profiling.Operation(trace, name);
			else
				return Profiling.Operation.Null;
		}

		static int GetMaxBufferSize(double viewSize)
		{
			return (int)Math.Ceiling(viewSize) + 1;
		}

		readonly bool disableSingleLogPositioningOptimization;
		readonly LJTraceSource trace;
		OperationTracker currentOperationTracker;
		readonly bool profilingEnabled = true;

		double viewSize; // size of the view the screen buffer needs to fill. nr of lines.
		bool isRawLogMode;

		// todo: (wrongly) computed value
		int bufferSize; // size of the buffer. it has enough messages to fill the view of size viewSize.

		Dictionary<IMessagesSource, SourceBuffer> buffers;
		double scrolledLines; // scrolling position as nr of lines. [0..1)

		// computed values
		List<ScreenBufferEntry> entries;
		IList<ScreenBufferEntry> entriesReadonly;

		readonly Diagnostics diagnostics = new Diagnostics();
	};
};
