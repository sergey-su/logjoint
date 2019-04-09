using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		internal static ViewLine ToViewLine(
			this ScreenBufferEntry e,
			MessageTextGetter displayTextGetter,
			bool showTime,
			bool showMilliseconds,
			(int, int) selectionViewLinesRange,
			SelectionInfo normalizedSelection,
			bool isBookmarked,
			Settings.Appearance.ColoringMode coloring,
			int? cursorCharIndex,
			IHighlightingHandler searchResultHighlightingHandler,
			IHighlightingHandler selectionHighlightingHandler,
			IHighlightingHandler highlightingFiltersHandler
		)
		{
			var msg = e.Message;
			var text = displayTextGetter(msg);
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
				BackgroundColor =
					coloring == Settings.Appearance.ColoringMode.None || msg.Thread == null || msg.Thread.IsDisposed ? new ModelColor?() :
					coloring == Settings.Appearance.ColoringMode.Threads ? msg.Thread.ThreadColor :
					coloring == Settings.Appearance.ColoringMode.Sources && msg.TryGetLogSource(out var ls) ? ls.Color :
					new ModelColor?(),
				IsBookmarked = isBookmarked,
				HasMessageSeparator = text.IsMultiline && text.GetLinesCount() == e.TextLineIndex + 1,
				SelectedBackground = GetSelection(e.Index, textLine, selectionViewLinesRange, normalizedSelection),
				CursorCharIndex = cursorCharIndex,
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