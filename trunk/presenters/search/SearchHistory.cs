using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	class SearchHistory
	{
		static public int MaxItemsCount
		{
			get { return 15; }
		}
		public void Add(string entry)
		{
			if (entry == null)
				throw new ArgumentNullException("entry");
			if (entry.Trim().Length == 0)
				return;
			items.RemoveAll(delegate(string i) { return i == entry; });
			if (items.Count < MaxItemsCount)
				items.Add(entry);
		}
		public IEnumerable<string> Items
		{
			get
			{
				for (int i = items.Count - 1; i >= 0; --i)
					yield return items[i]; 
			}
		}

		private List<string> items = new List<string>(MaxItemsCount);
	}
}
