using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	class Diagnostics
	{
		private readonly bool isEnabled = Debugger.IsAttached;

		void Assert(bool cond)
		{
			if (!cond)
				throw new InvalidOperationException();
		}

		public bool IsEnabled { get { return isEnabled; } }

		public void VerifyLines(IEnumerable<DisplayLine> lines, bool verifyConsecutiveMessages)
		{
			VerifyLines(lines.Select(l => new ScreenBufferEntry()
			{
				Index = l.Index,
				Message = l.Message,
				TextLineIndex = l.LineIndex
			}), verifyConsecutiveMessages);
		}

		public void VerifyLines(IEnumerable<ScreenBufferEntry> entries, bool verifyConsecutiveMessages)
		{
			if (!isEnabled)
				return;
			IMessage lastMessage = null;
			int lastLineIdx = -1;
			foreach (var e in entries)
			{
				if (e.Message != lastMessage)
				{
					if (lastMessage != null && verifyConsecutiveMessages)
						Assert(lastMessage.EndPosition == e.Message.Position);
					lastMessage = e.Message;
				}
				else
				{
					Assert(e.TextLineIndex == lastLineIdx + 1);
				}
				lastLineIdx = e.TextLineIndex;
			}
		}
	};
};