using LogJoint.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		internal static ViewLine ToViewLine(
			this ScreenBufferEntry e,
			MultilineText text,
			bool showTime,
			bool showMilliseconds,
			(int, int) selectionViewLinesRange,
			SelectionInfo normalizedSelection,
			bool isBookmarked,
			Settings.Appearance.ColoringMode coloring,
			ImmutableArray<Color> threadColors,
			int? cursorCharIndex,
			bool cursorVisible,
			IHighlightingHandler searchResultHighlightingHandler,
			IHighlightingHandler selectionHighlightingHandler,
			IHighlightingHandler highlightingFiltersHandler
		)
		{
			var msg = e.Message;
			var textLine = text.GetNthTextLine(e.TextLineIndex);
			return new ViewLine()
			{
				Message = msg,
				LineIndex = e.Index,
				Text = text,
				TextLineIndex = e.TextLineIndex,
				TextLineValue = textLine.Value,
				Time = showTime && e.TextLineIndex == 0 ? msg.Time.ToUserFrendlyString(showMilliseconds) : null,
				Severity =
					e.TextLineIndex != 0 ? SeverityIcon.None :
					msg.Severity == SeverityFlag.Error ? SeverityIcon.Error :
					msg.Severity == SeverityFlag.Warning ? SeverityIcon.Warning :
					SeverityIcon.None,
				ContextColor =
					coloring == Settings.Appearance.ColoringMode.None || msg.Thread == null || msg.Thread.IsDisposed ? new Color?() :
					coloring == Settings.Appearance.ColoringMode.Threads ? threadColors.GetByIndex(msg.Thread.ThreadColorIndex) :
					coloring == Settings.Appearance.ColoringMode.Sources && msg.TryGetLogSource(out var ls) ? threadColors.GetByIndex(ls.ColorIndex) :
					new Color?(),
				IsBookmarked = isBookmarked,
				HasMessageSeparator = text.IsMultiline && text.GetLinesCount() == e.TextLineIndex + 1,
				SelectedBackground = GetSelection(e.Index, textLine, selectionViewLinesRange, normalizedSelection),
				CursorCharIndex = cursorCharIndex,
				CursorVisible = cursorVisible,
				searchResultHighlightingHandler = searchResultHighlightingHandler,
				selectionHighlightingHandler = selectionHighlightingHandler,
				highlightingFiltersHandler = highlightingFiltersHandler
			};
		}

		private static (int, int)? GetSelection(int displayIndex, StringSlice line, (int first, int last) selectionViewLinesRange, SelectionInfo normalizedSelection)
		{
			if (normalizedSelection != null
			 && !normalizedSelection.IsEmpty
			 && displayIndex >= selectionViewLinesRange.first
			 && displayIndex <= selectionViewLinesRange.last)
			{
				int selectionStartIdx;
				int selectionEndIdx;
				if (displayIndex == selectionViewLinesRange.first)
					selectionStartIdx = normalizedSelection.First.LineCharIndex;
				else
					selectionStartIdx = 0;
				if (displayIndex == selectionViewLinesRange.last)
					selectionEndIdx = normalizedSelection.Last.LineCharIndex;
				else
					selectionEndIdx = line.Length;
				if (selectionStartIdx < selectionEndIdx && selectionStartIdx >= 0 && selectionEndIdx <= line.Value.Length)
				{
					return (selectionStartIdx, selectionEndIdx);
				}
			}
			return null;
		}

		public static async Task<bool> SetTopLineScrollValue(this IScreenBuffer screenBuffer, double value, CancellationToken cancellation)
		{
			if (value < 0 || value >= 1d)
				throw new ArgumentOutOfRangeException("value");
			double delta = value - screenBuffer.TopLineScrollValue;
			if (Math.Abs(delta) < 1e-3)
				return true;
			return Math.Abs(delta - await screenBuffer.ShiftBy(delta, cancellation)) < 1e-3;
		}
	};
};