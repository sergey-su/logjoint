using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	public interface ISearchHistory
	{
		event EventHandler OnChanged;
		void Add(SearchHistoryEntry entry);
		IEnumerable<SearchHistoryEntry> Items { get; }
		int Count { get; }
		int MaxCount { get; set; }
		void Clear();
	};
}
