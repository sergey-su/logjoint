using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	public struct SelectionInfo
	{
		public CursorPosition First { get { return first; } }
		public CursorPosition Last { get { return last; } }

		public int Version { get; set; } // todo: convert object to immutable ref class

		public bool IsValid => First.IsValid;

		public bool IsEmpty // todo: cache result
		{
			get
			{
				if (!First.IsValid || !Last.IsValid)
					return true;
				return CursorPosition.Compare(First, Last) == 0;
			}
		}

		public bool IsSingleLine
		{
			get
			{
				if (!First.IsValid || !Last.IsValid) // no selection 
					return false;
				return First.Message == Last.Message && First.TextLineIndex == Last.TextLineIndex;
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

		public CursorPosition first;
		public CursorPosition last;
		public bool normalized;
	};
};