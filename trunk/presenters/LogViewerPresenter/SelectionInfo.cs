using System;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal class SelectionInfo
	{
		public CursorPosition First => first;
		public CursorPosition Last => last;
		public MessageTextGetter MessageTextGetter => messageTextGetter;

		public SelectionInfo(CursorPosition f, CursorPosition l, MessageTextGetter messageTextGetter)
		{
			this.first = f ?? throw new ArgumentNullException("first");
			this.last = l;
			this.messageTextGetter = messageTextGetter;
		}

		public bool IsEmpty // todo: cache result
		{
			get
			{
				if (Last == null)
					return true;
				return CursorPosition.Compare(First, Last) == 0;
			}
		}

		public bool IsSingleLine
		{
			get
			{
				if (Last == null) // no range selection 
					return false;
				return First.Message == Last.Message && First.TextLineIndex == Last.TextLineIndex;
			}
		}

		public bool Contains(CursorPosition pos)
		{
			if (pos == null)
				throw new ArgumentNullException();
			if (IsEmpty)
				return false;
			var normalized = this.Normalize();
			return CursorPosition.Compare(normalized.First, pos) <= 0 && CursorPosition.Compare(normalized.Last, pos) >= 0;
		}

		public SelectionInfo Normalize()
		{
			if (last == null || CursorPosition.Compare(first, last) <= 0)
				return this;
			else
				return new SelectionInfo(last, first, messageTextGetter);
		}

		private readonly CursorPosition first;
		private readonly CursorPosition last;
		private readonly MessageTextGetter messageTextGetter;
	};
};