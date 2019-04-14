using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		internal static ViewLine ToViewLine(
			this ScreenBufferEntry e,
			bool rawMode,
			bool showTime,
			bool showMilliseconds,
			bool isBookmarked = false,
			Settings.Appearance.ColoringMode coloring = Settings.Appearance.ColoringMode.None,
			SelectionInfo? normalizedValidSelection = null,
			bool cursorState = false,
			IHighlightingHandler searchResultHighlightingHandler = null,
			IHighlightingHandler selectionHighlightingHandler = null,
			IHighlightingHandler highlightingFiltersHandler = null
		)
		{
			var msg = e.Message;
			var text = msg.GetDisplayText(rawMode);
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
				SelectedBackground = normalizedValidSelection != null && !normalizedValidSelection.Value.IsEmpty ? GetSelection(e.Index, textLine, normalizedValidSelection.Value) : null,
				HasMessageSeparator = text.IsMultiline && text.GetLinesCount() == e.TextLineIndex + 1,
				CursorCharIndex = cursorState && normalizedValidSelection != null && normalizedValidSelection.Value.First.DisplayIndex == e.Index ? normalizedValidSelection.Value.First.LineCharIndex : new int?(),
				searchResultHighlightingHandler = searchResultHighlightingHandler,
				selectionHighlightingHandler = selectionHighlightingHandler,
				highlightingFiltersHandler = highlightingFiltersHandler
			};
		}

		private static (int, int)? GetSelection(int displayIndex, StringSlice line, SelectionInfo normalizedSelection)
		{
			if (!normalizedSelection.IsEmpty
			 && displayIndex >= normalizedSelection.First.DisplayIndex
			 && displayIndex <= normalizedSelection.Last.DisplayIndex)
			{
				int selectionStartIdx;
				int selectionEndIdx;
				if (displayIndex == normalizedSelection.First.DisplayIndex)
					selectionStartIdx = normalizedSelection.First.LineCharIndex;
				else
					selectionStartIdx = 0;
				if (displayIndex == normalizedSelection.Last.DisplayIndex)
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