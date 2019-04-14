namespace LogJoint.UI.Presenters.LogViewer
{
	public struct CursorPosition
	{
		internal IMessage Message;
		public IMessagesSource Source;
		public int DisplayIndex; // todo: reactive update
		public int TextLineIndex;
		public int LineCharIndex;

		public bool IsValid => Message != null;

		public static int Compare(CursorPosition p1, CursorPosition p2)
		{
			if (!p1.IsValid && !p2.IsValid)
				return 0;
			if (!p1.IsValid)
				return -1;
			if (!p2.IsValid)
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

		public static CursorPosition FromViewLine(ViewLine l, int charIndex)
		{
			return new CursorPosition()
			{
				Message = l.Message,
				DisplayIndex = l.LineIndex,
				TextLineIndex = l.TextLineIndex,
				LineCharIndex = charIndex
			};
		}
	};
};