using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		public static ViewLine ToViewLine(this ScreenBufferEntry e,
			bool rawMode, bool showTime, bool showMilliseconds, Settings.Appearance.ColoringMode coloring = Settings.Appearance.ColoringMode.None)
		{
			var msg = e.Message;
			return new ViewLine()
			{
				Message = msg,
				LineIndex = e.Index,
				Text = msg.GetDisplayText(rawMode),
				TextLineIndex = e.TextLineIndex,
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
					new ModelColor?()
			};
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