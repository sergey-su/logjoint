using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using LogJoint.Postprocessing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class ScreenBuffer: IScreenBuffer
	{
		internal ScreenBuffer(
			IChangeNotification changeNotification,
			double viewSize,
			LJTraceSource trace = null,
			bool disableSingleLogPositioningOptimization = false
		)
		{
			this.changeNotification = changeNotification;
			this.buffers = new Dictionary<IMessagesSource, SourceBuffer>();
			this.entries = ImmutableArray.Create<ScreenBufferEntry>();
			this.disableSingleLogPositioningOptimization = disableSingleLogPositioningOptimization;
			this.trace = trace ?? LJTraceSource.EmptyTracer;
			this.sources = Selectors.Create(
				() => buffersVersion,
				_ => (IReadOnlyList<SourceScreenBuffer>)ImmutableArray.CreateRange(buffers.Select(b => new SourceScreenBuffer
				{
					Source = b.Key,
					Begin = b.Value.BeginPosition,
					End = b.Value.EndPosition
				}))
			);
			this.bufferPosition = CreateBufferPositionSelector();
			this.SetViewSize(viewSize);
		}

		double IScreenBuffer.ViewSize { get { return viewSize; } }

		async Task IScreenBuffer.SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation)
		{
			string opName = "SetSources";
			var currentTop = EnumScreenBufferLines().FirstOrDefault();

			var oldBuffers = buffers;
			buffers = sources.ToDictionary(s => s, s => oldBuffers.ContainsKey(s) ? oldBuffers[s] : new SourceBuffer(s, diagnostics, displayTextGetter));
			if (!buffers.Keys.ToHashSet().SetEquals(oldBuffers.Keys))
			{
				buffersVersion++;
				changeNotification.Post();
			}

			if (!currentTop.IsEmpty)
			{
				var currentTopSourcePresent = buffers.ContainsKey(currentTop.Source);

				await PerformBuffersTransaction(
					opName,
					cancellation,
					modifyBuffers: tmp => Task.WhenAll(tmp.Select(s => oldBuffers.ContainsKey(s.Source) ?
						s.LoadAround(GetMaxBufferSize(viewSize), cancellation) :
						s.LoadAt(currentTop.Message.Time.ToLocalDateTime(), GetMaxBufferSize(viewSize), cancellation)
					)),
					getPivotLine: currentTopSourcePresent ? MakePivotLineGetter(l =>
					{
						if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex)
							return -scrolledLines;
						return null;
					}) : (lines, bufs) =>
					{
						var best = lines
							.Select(l => new { l, diff = (l.Message.Time.ToLocalDateTime() - currentTop.Message.Time.ToLocalDateTime()).Abs() })
							.Aggregate(new { l = new DisplayLine(), diff = TimeSpan.MaxValue }, (acc, l) => l.diff < acc.diff ? l : acc);
						return !best.l.IsEmpty ? Tuple.Create(best.l, 0d) : null;
					}
				);
			}
			else
			{
				await PerformBuffersTransaction(
					opName,
					cancellation,
					modifyBuffers: tmp => Task.FromResult(0),
					getPivotLine: MakePivotLineGetter(l => 0)
				);
			}
		}

		Task IScreenBuffer.SetViewSize(double sz, CancellationToken cancellation)
		{
			SetViewSize(sz);
			var currentTop = EnumScreenBufferLines().FirstOrDefault(); 			return PerformBuffersTransaction(
				string.Format("SetViewSize({0})", sz),
				cancellation, 				modifyBuffers: tmp => Task.WhenAll(tmp.Select(b => b.LoadAround(GetMaxBufferSize(sz), cancellation))), 				getPivotLine: MakePivotLineGetter(l => 				{ 					if (currentTop.IsEmpty) 						return 0; 					if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex) 						return -scrolledLines; 					return null; 				})
			); 		}

		Task IScreenBuffer.SetDisplayTextGetter(MessageTextGetter displayTextGetter, CancellationToken cancellation)
		{
			if (this.displayTextGetter == displayTextGetter)
				return Task.FromResult(0);
			this.displayTextGetter = displayTextGetter;
			changeNotification.Post();
			var currentTop = EnumScreenBufferLines().FirstOrDefault();
			return PerformBuffersTransaction(
				string.Format("SetDisplayTextGetter({0})", displayTextGetter),
				cancellation, 				modifyBuffers: tmp => Task.WhenAll(tmp.Select(b => b.LoadAround(GetMaxBufferSize(viewSize), cancellation))), 				getPivotLine: (lines, bufs) => 				{ 					var candidate = new DisplayLine();
					if (!currentTop.IsEmpty)
					{
						candidate = lines.FirstOrDefault(l => MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex); 						if (candidate.IsEmpty)
							candidate = lines.FirstOrDefault(l => MessagesComparer.Compare(l.Message, currentTop.Message) == 0);
					}
					if (candidate.IsEmpty)
						candidate = lines.FirstOrDefault();
					if (candidate.IsEmpty)
						return null; 					return Tuple.Create(candidate, -scrolledLines); 				} 			); 		}

		MessageTextGetter IScreenBuffer.DisplayTextGetter { get { return displayTextGetter; } }

		double IScreenBuffer.TopLineScrollValue 
		{
			get { return scrolledLines; }
		}

		void IScreenBuffer.MakeFirstLineFullyVisible()
		{
			SetScrolledLines(0);
		}

		IReadOnlyList<ScreenBufferEntry> IScreenBuffer.Messages => entries;

		IReadOnlyList<SourceScreenBuffer> IScreenBuffer.Sources => sources();

		Task IScreenBuffer.MoveToStreamsBegin(CancellationToken cancellation)
		{
			return PerformBuffersTransaction(
				"MoveToStreamsBegin",
				cancellation,
				modifyBuffers: tmp => Task.WhenAll(tmp.Select(b => {
					b.Reset(b.Source.PositionsRange.Begin);
					return b.LoadAfter(GetMaxBufferSize(viewSize), cancellation);
				})),
				getPivotLine: MakePivotLineGetter(l => 0)
			);
		}

		Task IScreenBuffer.MoveToStreamsEnd(CancellationToken cancellation)
		{
			return PerformBuffersTransaction(
				"MoveToStreamsEnd",
				cancellation,
				modifyBuffers: tmp => Task.WhenAll(tmp.Select(b =>
				{
					b.Reset(b.Source.PositionsRange.End);
					return b.LoadBefore(GetMaxBufferSize(viewSize), cancellation);
				})),
				getPivotLine: (lines, bufs) =>
				{
					return lines.Count > 0 ? Tuple.Create(lines.Last(), viewSize - 1) : null;
				}
			);
		}

		async Task<bool> IScreenBuffer.MoveToBookmark(
			IBookmark bookmark,
			BookmarkLookupMode mode,
			CancellationToken cancellation)
		{
			var matchMode = mode & BookmarkLookupMode.MatchModeMask;
			Func<DisplayLine, int> cmp = (DisplayLine l) =>
			{
				var ret = MessagesComparer.CompareLogSourceConnectionIds(l.Message.GetConnectionId(), bookmark.LogSourceConnectionId);
				if (ret == 0)
					ret = Math.Sign(l.Message.Position - bookmark.Position);
				if (ret == 0)
					ret = Math.Sign(l.LineIndex - bookmark.LineIndex);
				return ret;
			};

			return await PerformBuffersTransaction(
				string.Format("MoveToBookmark({0})", mode),
				cancellation,
				modifyBuffers: tmp => Task.WhenAll(tmp.Select(buf =>
						matchMode == BookmarkLookupMode.ExactMatch && buf.Source.LogSourceHint?.ConnectionId == bookmark.LogSourceConnectionId ?
					buf.LoadAround(bookmark.Position, GetMaxBufferSize(viewSize) + bookmark.LineIndex, cancellation) :
					buf.LoadAt(bookmark.Time.ToLocalDateTime(), GetMaxBufferSize(viewSize) + bookmark.LineIndex, cancellation)
				)),
				getPivotLine: (lines, bufs) =>
				{
					DisplayLine ret = new DisplayLine();
					if (matchMode == BookmarkLookupMode.ExactMatch)
					{
						ret = lines.FirstOrDefault(l => cmp(l) == 0);
					}
					else if (matchMode == BookmarkLookupMode.FindNearestMessage)
					{
						ret = lines.FirstOrDefault(l => cmp(l) >= 0);
						if (ret.IsEmpty)
							ret = lines.LastOrDefault(l => cmp(l) < 0);
					}
					return ret.Message == null ? null : Tuple.Create(ret, ComputeMatchedLinePosition(mode));
				}
			) != null;
		}

		double ComputeMatchedLinePosition(BookmarkLookupMode mode)
		{
			return (mode & BookmarkLookupMode.MoveBookmarkToMiddleOfScreen) != 0 ? Math.Max(viewSize - 1d, 0) / 2d : 0d;
		}

		async Task<ScreenBufferEntry?> IScreenBuffer.MoveToTimestamp(
			DateTime timestamp,
			CancellationToken cancellation
		)
		{
			Func<IEnumerable<DisplayLine>, DisplayLine> findNearest = (lines) =>
			{
				return lines.MinByKey(l => (l.Message.Time.ToLocalDateTime() - timestamp).Abs());
			};

			if (await PerformBuffersTransaction(
				string.Format("MoveToTimestamp({0})", timestamp.ToString("O")),
				cancellation,
				modifyBuffers: tmp => Task.WhenAll(tmp.Select(buf =>
					buf.LoadAt(timestamp, GetMaxBufferSize(viewSize), cancellation)
				)),
				getPivotLine: (lines, bufs) =>
				{
					var ret = findNearest(lines);
					return ret.IsEmpty ? null :
						Tuple.Create(ret, ComputeMatchedLinePosition(BookmarkLookupMode.MoveBookmarkToMiddleOfScreen));
				}) == null
			)
			{
				return null;
			}

			var line = findNearest(EnumScreenBufferLines());
			if (line.IsEmpty)
				return null;

			return line.ToScreenBufferEntry();
		}

		async Task<double> IScreenBuffer.ShiftBy(double nrOfDisplayLines, CancellationToken cancellation)
		{
			var currentTop = EnumScreenBufferLines().FirstOrDefault();
			if (currentTop.IsEmpty)
				return 0;
			var saveScrolledLines = scrolledLines;

			var pivotLinePosition = await PerformBuffersTransaction(
				string.Format("ShiftBy({0})", nrOfDisplayLines),
				cancellation,
				modifyBuffers: tmp => Task.WhenAll(tmp.Select(buf =>
					buf.LoadAround(GetMaxBufferSize(viewSize + Math.Abs(nrOfDisplayLines)), cancellation)
				)),
				getPivotLine: MakePivotLineGetter(l =>
				{
					if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex)
						return -nrOfDisplayLines - scrolledLines;
					return null;
				})
			);
			if (!pivotLinePosition.HasValue)
				return 0;

			return -saveScrolledLines - pivotLinePosition.Value;
		}

		double IScreenBuffer.BufferPosition => bufferPosition();

		Task IScreenBuffer.MoveToPosition(
			double position,
			CancellationToken cancellation
		)
		{
			if (buffers.Count == 1 && !disableSingleLogPositioningOptimization)
				return MoveToPositionSingleLog(position, cancellation);
			else
				return MoveToPositionMultipleLogs(position, cancellation);
		}

		Task IScreenBuffer.Refresh(CancellationToken cancellation)
		{
			var currentTop = EnumScreenBufferLines().FirstOrDefault(); 			return PerformBuffersTransaction(
				string.Format("Refresh()"),
				cancellation, 				modifyBuffers: tmp => Task.WhenAll(tmp.Select(b => b.LoadAround(GetMaxBufferSize(viewSize), cancellation))), 				getPivotLine: MakePivotLineGetter(l => 				{ 					if (currentTop.IsEmpty) 						return 0; 					if (MessagesComparer.Compare(l.Message, currentTop.Message) == 0 && l.LineIndex == currentTop.LineIndex) 						return -scrolledLines; 					return null; 				})
			);
		}

		public override string ToString()
		{
			var ret = new StringBuilder();
			foreach (var e in entries)
			{
				displayTextGetter(e.Message).GetNthTextLine(e.TextLineIndex).Append(ret);
				ret.AppendLine();
			}
			return ret.ToString();
		}

		Func<double> CreateBufferPositionSelector()
		{
			return Selectors.Create(
				() => buffersVersion,
				() => entries,
				() => viewSize,
				() => buffers.Values.Aggregate(0L, (agg, src) => agg + src.Source.ScrollPositionsRange.Length),
				(_1, _2, _3, totalScrollLength) =>
				{
					if (totalScrollLength == 0 || ViewIsTooSmall())
						return 0;
					foreach (var i in GetBufferZippedWithScrollPositions(buffers.Values, EnumScreenBufferLines(buffers.Values)))
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
			);
		}

		static IEnumerable<DisplayLine> EnumScreenBufferLines(IEnumerable<IMessagesCollection> colls)
		{
			return MakeMergingCollection(colls)
				.Forward(0, int.MaxValue)
				.Select(m => ((SourceBuffer)m.SourceCollection).Get(m.SourceIndex).MakeIndexed(m.Message.Index));
		}

		IEnumerable<DisplayLine> EnumScreenBufferLines()
		{
			return EnumScreenBufferLines(buffers.Values);
		}

		static MessagesContainers.MergingCollection MakeMergingCollection(IEnumerable<IMessagesCollection> colls)
		{
			return new MessagesContainers.SimpleMergingCollection(colls);
		}

		static IEnumerable<LineScrollInfo> GetBufferZippedWithScrollPositions(IEnumerable<SourceBuffer> bufs, IEnumerable<DisplayLine> lines)
		{
			var currentIndices = bufs.ToDictionary(b => b.Source, b => new Ref<int>());

			Func<LineScrollInfo> calcScrollPosHelper = () =>
			{
				var ret = new LineScrollInfo();

				foreach (var buf in bufs)
				{
					int currentIndex = currentIndices[buf.Source].Value;
					if (currentIndex < buf.Count)
					{
						var dl = buf.Get(currentIndex);
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
			};

			foreach (var m in lines)
			{
				var source = m.Source;
				var lineInfo = calcScrollPosHelper();
				lineInfo.Index = m.Index;
				yield return lineInfo;
				currentIndices[source].Value++;
			}
		}

		Task MoveToPositionSingleLog(
			double position,
			CancellationToken cancellation
		)
		{
			Func<SourceBuffer, double> getScrollPosition = buf =>
			{
				var scrollPosRange = buf.Source.ScrollPositionsRange;
				return scrollPosRange.Begin + position * (double)scrollPosRange.Length;
			};
			return PerformBuffersTransaction(
				string.Format("MoveToPosition(single, {0})", position),
				cancellation,
				modifyBuffers: tmp =>
				{
					var buf = tmp.Single();
					return buf.LoadAround(buf.Source.MapScrollPositionToPosition((long)getScrollPosition(buf)),
						GetMaxBufferSize(viewSize), cancellation,
						doNotCountFirstMessage: true);
				},
				getPivotLine: (lines, bufs) => GetPivotLineForScrolling(lines, bufs, position, getScrollPosition(bufs.Single()))
			);
		}

		private Tuple<DisplayLine, double> GetPivotLineForScrolling(List<DisplayLine> lines, IEnumerable<SourceBuffer> bufs, double position, double scrollPosition)
		{
			foreach (var i in GetBufferZippedWithScrollPositions(bufs, lines))
			{
				if (i.ScrollPositionEnd >= scrollPosition)
				{
					double linePortion = Math.Max(0, (scrollPosition - i.ScrollPositionBegin) / (i.ScrollPositionEnd - i.ScrollPositionBegin));
					double targetViewPortion = position * viewSize - linePortion;

					return Tuple.Create(lines[i.Index], targetViewPortion);
				}
			}
			return null;
		}

		Task MoveToPositionMultipleLogs(
			double position,
			CancellationToken cancellation
		)
		{
			Func<IEnumerable<SourceBuffer>, double> getFlatLogPosition = bufs =>
			{
				long fullPositionsRangeLength = bufs.Select(b => b.Source.ScrollPositionsRange.Length).Sum();
				return position * (double)fullPositionsRangeLength;
			};
			return PerformBuffersTransaction(
				string.Format("MoveToPosition(multiple, {0})", position),
				cancellation,
				modifyBuffers: async tmp =>
				{
					var fullDatesRange = tmp.Aggregate(DateRange.MakeEmpty(), (agg, s) => DateRange.Union(agg, s.Source.DatesRange));
					var flatLogPosition = getFlatLogPosition(tmp);
					var searchRange = new ListUtils.VirtualList<DateTime>(
						(int)fullDatesRange.Length.TotalMilliseconds, i => fullDatesRange.Begin.AddMilliseconds(i));
					var bufferSize = GetMaxBufferSize(viewSize);
					var ms = await searchRange.BinarySearchAsync(0, searchRange.Count, async d =>
					{
						long datePosition = 0;
						foreach (var b in tmp)
						{
							var dateBound = await b.Source.GetDateBoundPosition(
								d, ValueBound.Upper, LogProviderCommandPriority.RealtimeUserAction, cancellation);
							cancellation.ThrowIfCancellationRequested();
							datePosition += b.Source.MapPositionToScrollPosition(dateBound.Position);
						}
						return datePosition <= flatLogPosition;
					}) - 1;
					cancellation.ThrowIfCancellationRequested();

					var date = fullDatesRange.Begin.AddMilliseconds(ms);
					await Task.WhenAll(tmp.Select(s => s.LoadAt(date, bufferSize, cancellation)));
				},
				getPivotLine: (lines, bufs) => GetPivotLineForScrolling(lines, bufs, position, getFlatLogPosition(bufs))
			);
		}

		private void SetViewSize(double sz)
		{
			if (sz < 0)
				throw new ArgumentOutOfRangeException("view size");
			viewSize = sz;
			changeNotification.Post();
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
		};


		void SetScrolledLines(double value)
		{
			if (value < 0 || value >= 1d)
				throw new ArgumentOutOfRangeException();
			scrolledLines = value;
			changeNotification.Post();
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

		static Func<List<DisplayLine>, IEnumerable<SourceBuffer>, Tuple<DisplayLine, double>> MakePivotLineGetter(
			Func<DisplayLine, double?> testPivotLine)
		{
			return (lines, bufs) =>
			{
				foreach (var l in lines)
				{
					var testResult = testPivotLine(l);
					if (testResult != null)
						return Tuple.Create(l, testResult.Value);
				}
				return null;
			};
		}

		async Task<double?> PerformBuffersTransaction(
			string name,
			CancellationToken cancellation,
			Func<IEnumerable<SourceBuffer>, Task> modifyBuffers,
			Func<List<DisplayLine>, IEnumerable<SourceBuffer>, Tuple<DisplayLine, double>> getPivotLine)
		{
			using (name != null ? CreateTrackerForNewOperation(name, cancellation) : null)
			{
				var tmpCopy = buffers.ToDictionary(s => s.Key, s => new SourceBuffer(s.Value, displayTextGetter));
				await modifyBuffers(tmpCopy.Values);
				cancellation.ThrowIfCancellationRequested();
				return FinalizeTransaction(tmpCopy, getPivotLine);
			}
		}

		double? FinalizeTransaction(Dictionary<IMessagesSource, SourceBuffer> tmpCopy, Func<List<DisplayLine>, IEnumerable<SourceBuffer>, Tuple<DisplayLine, double>> getPivotLine)
		{
			var lines = EnumScreenBufferLines(tmpCopy.Values).ToList();

			if (lines.Count == 0)
			{
				if (entries.Length > 0)
				{
					entries = entries.Clear();
					SetScrolledLines(0);
					changeNotification.Post();
				}
				return null;
			}

			var pivotLine = getPivotLine(lines, tmpCopy.Values);
			foreach (var line in lines)
			{
				if (pivotLine != null && line.Index == pivotLine.Item1.Index)
				{
					double idx = pivotLine.Item2;
					int idxWhole = (int)Math.Ceiling(idx);

					int topLineIdx = line.Index - idxWhole;
					double topLineScroll = idxWhole - idx;
					double ret = idx;

					Action<int, double> applyConstraint = (int newTopLineIdx, double newTopLineScroll) =>
					{
						ret += ((double)topLineIdx - topLineScroll) - ((double)newTopLineIdx - newTopLineScroll);
						topLineIdx = newTopLineIdx;
						topLineScroll = newTopLineScroll;
					};

					var bufferSize = (int)Math.Ceiling(viewSize + topLineScroll);

					if (topLineIdx + topLineScroll + viewSize > lines.Count)
					{
						applyConstraint(lines.Count - (int)Math.Ceiling(viewSize), Math.Ceiling(viewSize) - viewSize);
					}
					if (topLineIdx < 0)
					{
						applyConstraint(0, 0);
					}

					var unnededTopMessages = tmpCopy.Keys.ToDictionary(s => s, s => new Ref<int>());
					foreach (var l in lines)
					{
						if (l.Index >= topLineIdx)
							break;
						else
							unnededTopMessages[l.Source].Value++;
					}

					foreach (var buf in tmpCopy.Values)
					{
						buf.Cut(unnededTopMessages[buf.Source].Value, bufferSize);
					}

					buffers = tmpCopy;
					entries = ImmutableArray.CreateRange(MakeMergingCollection(tmpCopy.Values).Forward(0, bufferSize).Select(ToScreenBufferMessage));
					changeNotification.Post();

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

		readonly IChangeNotification changeNotification;
		readonly bool disableSingleLogPositioningOptimization;
		readonly LJTraceSource trace;
		OperationTracker currentOperationTracker;
		readonly bool profilingEnabled = true;

		double viewSize; // size of the view the screen buffer needs to fill. nr of lines.
		MessageTextGetter displayTextGetter = MessageTextGetters.SummaryTextGetter;

		Dictionary<IMessagesSource, SourceBuffer> buffers;
		int buffersVersion;
		Func<IReadOnlyList<SourceScreenBuffer>> sources;
		double scrolledLines; // scrolling position as nr of lines. [0..1)

		// computed values
		ImmutableArray<ScreenBufferEntry> entries;
		Func<double> bufferPosition;

		readonly Diagnostics diagnostics = new Diagnostics();
	};
};
