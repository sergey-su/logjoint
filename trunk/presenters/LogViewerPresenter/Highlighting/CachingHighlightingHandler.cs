using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.LogViewer
{
	class CachingHighlightingHandler : IHighlightingHandler
	{
		readonly LRUCache<IMessage, RangeTree.IRangeTree<int, HighlightRange>> cache;
		readonly Func<IMessage, IEnumerable<(int, int, ModelColor)>> getRangesForMessage;

		public CachingHighlightingHandler(
			Func<IMessage, IEnumerable<(int, int, ModelColor)>> getRangesForMessage,
			int cacheSize
		)
		{
			this.getRangesForMessage = getRangesForMessage;
			this.cache = new LRUCache<IMessage, RangeTree.IRangeTree<int, HighlightRange>>(cacheSize);
		}

		IEnumerable<(int, int, ModelColor)> IHighlightingHandler.GetHighlightingRanges(ViewLine vl)
		{
			if (!cache.TryGetValue(vl.Message, out var item))
			{
				cache.Set(vl.Message, item = new RangeTree.RangeTree<int, HighlightRange>(getRangesForMessage(vl.Message).Select(
					r => new HighlightRange()
					{
						Range = new RangeTree.Range<int>(r.Item1, r.Item2),
						Color = r.Item3
					}
				), HighlightRange.Comparer.Instance));
			}
			var line = vl.Text.GetNthTextLine(vl.TextLineIndex);
			int lineBegin = line.StartIndex - vl.Text.Text.StartIndex;
			int lineEnd = lineBegin + line.Length;
			return
				item
				.Query(new RangeTree.Range<int>(lineBegin, lineEnd))
				.Select(i => (i.Range.From, i.Range.To, i.Color))
				.Select(hlRange =>
				{
					int? hlBegin = null;
					int? hlEnd = null;
					if (hlRange.From >= lineBegin && hlRange.From <= lineEnd)
						hlBegin = hlRange.From;
					if (hlRange.To >= lineBegin && hlRange.To <= lineEnd)
						hlEnd = hlRange.To;
					return (hlBegin, hlEnd, hlRange.Color);
				})
				.Select(i => (
					i.hlBegin.GetValueOrDefault(lineBegin) - lineBegin,
					i.hlEnd.GetValueOrDefault(lineEnd) - lineBegin,
					i.Color
				));
		}

		class HighlightRange : RangeTree.IRangeProvider<int>
		{
			public RangeTree.Range<int> Range { get; set; }
			public ModelColor Color { get; set; }

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