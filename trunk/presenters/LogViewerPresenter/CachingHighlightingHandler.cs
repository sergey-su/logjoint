using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.LogViewer
{
	class CachingHighlightingHandler : IHighlightingHandler
	{
		readonly LRUCache<IMessage, RangeTree.IRangeTree<int, HighlightRange>> cache;
		readonly Func<IMessage, IEnumerable<(int, int, FilterAction)>> getRangesForMessage;

		public CachingHighlightingHandler(
			Func<IMessage, IEnumerable<(int, int, FilterAction)>> getRangesForMessage,
			int cacheSize
		)
		{
			this.getRangesForMessage = getRangesForMessage;
			this.cache = new LRUCache<IMessage, RangeTree.IRangeTree<int, HighlightRange>>(cacheSize);
		}

		IEnumerable<(int, int, FilterAction)> IHighlightingHandler.GetHighlightingRanges(IMessage msg, int intervalBegin, int intervalEnd)
		{
			if (!cache.TryGetValue(msg, out var item))
			{
				cache.Set(msg, item = new RangeTree.RangeTree<int, HighlightRange>(getRangesForMessage(msg).Select(
					r => new HighlightRange()
					{
						Range = new RangeTree.Range<int>(r.Item1, r.Item2),
						Action = r.Item3
					}
				), HighlightRange.Comparer.Instance));
			}
			return item.Query(new RangeTree.Range<int>(intervalBegin, intervalEnd)).Select(i => (i.Range.From, i.Range.To, i.Action));
		}

		class HighlightRange : RangeTree.IRangeProvider<int>
		{
			public RangeTree.Range<int> Range { get; set; }
			public FilterAction Action { get; set; }

			public class Comparer : IComparer<HighlightRange>
			{
				public static readonly Comparer Instance = new Comparer();

				public int Compare(HighlightRange x, HighlightRange y)
				{
					return x.Range.CompareTo(y.Range);
				}
			}
		};
	};
};