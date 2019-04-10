using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		public static ViewLine ToViewLine(this ScreenBufferEntry e, bool rawMode, bool showTime, bool showMilliseconds)
		{
			return new ViewLine()
			{
				Message = e.Message,
				LineIndex = e.Index,
				Text = e.Message.GetDisplayText(rawMode),
				TextLineIndex = e.TextLineIndex,
				Time = showTime && e.TextLineIndex == 0 ? e.Message.Time.ToUserFrendlyString(showMilliseconds) : null,
				Severity =
					e.TextLineIndex != 0 ? SeverityIcon.None :
					e.Message.Severity == SeverityFlag.Error ? SeverityIcon.Error :
					e.Message.Severity == SeverityFlag.Warning ? SeverityIcon.Warning :
					SeverityIcon.None
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