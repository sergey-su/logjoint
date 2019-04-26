using System;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal class SelectionInfo
	{
		public CursorPosition First => first;
		public CursorPosition Last => last;
		public MessageTextGetter MessageTextGetter => messageTextGetter;

		public SelectionInfo(CursorPosition first, CursorPosition last, MessageTextGetter messageTextGetter)
		{
			this.first = first ?? throw new ArgumentNullException(nameof(first));
			this.last = last;
			this.messageTextGetter = messageTextGetter;
		}

		public bool IsEmpty
		{
			get
			{
				if (!isEmpty.HasValue)
					isEmpty = Last == null || CursorPosition.Compare(First, Last) == 0;
				return isEmpty.Value;
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
		private bool? isEmpty;
	};
};