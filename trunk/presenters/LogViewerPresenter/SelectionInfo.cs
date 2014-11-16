using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	public struct SelectionInfo
	{
		public CursorPosition First { get { return first; } }
		public CursorPosition Last { get { return last; } }

		public IMessage Message { get { return First.Message; } }
		public int DisplayPosition { get { return First.DisplayIndex; } }

		public void SetSelection(CursorPosition begin, CursorPosition? end)
		{
			this.first = begin;
			if (end.HasValue)
				this.last = end.Value;
			normalized = CursorPosition.Compare(first, last) <= 0;
		}

		public bool IsEmpty
		{
			get
			{
				if (First.Message == null || Last.Message == null)
					return true;
				return CursorPosition.Compare(First, Last) == 0;
			}
		}

		public bool IsSingleLine
		{
			get
			{
				if (First.Message == null || Last.Message == null) // no selection 
					return false;
				return First.DisplayIndex == Last.DisplayIndex && First.TextLineIndex == Last.TextLineIndex;
			}
		}

		public bool IsInsideSelection(CursorPosition pos)
		{
			var normalized = this.Normalize();
			if (normalized.IsEmpty)
				return false;
			return CursorPosition.Compare(normalized.First, pos) <= 0 && CursorPosition.Compare(normalized.Last, pos) >= 0;
		}

		public SelectionInfo Normalize()
		{
			if (normalized)
				return this;
			else
				return new SelectionInfo { first = last, last = first, normalized = true };
		}

		public IEnumerable<int> GetDisplayIndexesRange()
		{
			if (IsEmpty)
			{
				yield return first.DisplayIndex;
			}
			else
			{
				SelectionInfo norm = Normalize();
				for (int i = norm.first.DisplayIndex; i <= norm.last.DisplayIndex; ++i)
					yield return i;
			}
		}

		CursorPosition first;
		CursorPosition last;
		bool normalized;
	};
};