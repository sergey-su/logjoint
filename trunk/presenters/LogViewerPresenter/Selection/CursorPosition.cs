using System;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal class CursorPosition
	{
		internal readonly IMessage Message;
		public readonly IMessagesSource Source;
		public readonly int TextLineIndex;
		public readonly int LineCharIndex;

		public static int Compare(CursorPosition p1, CursorPosition p2)
		{
			if (p1 == null && p2 == null)
				return 0;
			if (p1 == null)
				return -1;
			if (p2 == null)
				return 1;
			int i;
			i = MessagesComparer.Compare(p1.Message, p2.Message);
			if (i != 0)
				return i;
			i = p1.TextLineIndex - p2.TextLineIndex;
			if (i != 0)
				return i;
			i = p1.LineCharIndex - p2.LineCharIndex;
			return i;
		}

		public static CursorPosition FromScreenBufferEntry(ScreenBufferEntry l, int charIndex)
		{
			return new CursorPosition(l.Message, l.Source, l.TextLineIndex, charIndex);
		}

		internal CursorPosition(IMessage message, IMessagesSource source, int textLineIndex, int lineCharIndex)
		{
			Message = message ?? throw new ArgumentNullException("message");
			Source = source ?? throw new ArgumentNullException("source");
			TextLineIndex = textLineIndex;
			LineCharIndex = lineCharIndex;
		}
	};
};