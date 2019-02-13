using System;
using System.Collections.Generic;
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

		public void VerifyLines(IEnumerable<ScreenBufferEntry> entries)
		{
			IMessage lastMessage = null;
			int lastLineIdx = -1;
			foreach (var e in entries)
			{
				if (e.Message != lastMessage)
				{
					if (lastMessage != null)
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