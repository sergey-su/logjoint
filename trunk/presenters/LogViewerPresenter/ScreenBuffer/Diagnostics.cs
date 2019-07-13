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
			VerifyLines(lines.Select(l => l.ToScreenBufferEntry()), verifyConsecutiveMessages);
		}

		public void VerifyPositionsOrderBeforeRangeConcatenation(long end, long begin, bool mustBeConsecutiveMessages)
		{
			if (!isEnabled)
				return;
			if (mustBeConsecutiveMessages)
				Assert(begin == end);
			else
				Assert(begin >= end);
		}

		public void VerifyLines(IEnumerable<ScreenBufferEntry> entries, bool verifyConsecutiveMessages)
		{
			if (!isEnabled)
				return;
			IMessage lastMessage = null;
			int lastLineIdx = -1;
			var lastMessages = new Dictionary<IMessagesSource, IMessage>();
			foreach (var e in entries)
			{
				if (e.Message != lastMessage)
				{
					if (verifyConsecutiveMessages && lastMessages.TryGetValue(e.Source, out var lastSourceMessage))
						Assert(lastSourceMessage.EndPosition == e.Message.Position);
					lastMessages[e.Source] = e.Message;
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