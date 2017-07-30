using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	public static class FiltersExtensions
	{
		public static List<IFilter> GetPositiveFilters(this IFiltersList filters)
		{
			var positiveFilters = filters.Items.Where(f => f.Enabled && f.Action != FilterAction.Exclude).ToList();
			if (filters.GetDefaultAction() != FilterAction.Exclude)
				positiveFilters.Add(null);
			return positiveFilters;
		}

		public static IEnumerable<ILogSource> GetScopeSources(this IEnumerable<ILogSource> sources, List<IFilter> positiveFilters)
		{
			return sources.Where(s => positiveFilters.Any(f =>
				f == null || f.Options.Scope.ContainsAnythingFromSource(s)));
		}

		static readonly ModelColor[] actionColors = MakeActionColors();

		static ModelColor[] MakeActionColors()
		{
			var ret = (new HighlightBackgroundColorsGenerator()).Items.ToArray();
			if (ret.Length < (FilterAction.IncludeAndColorizeLast - FilterAction.IncludeAndColorizeFirst + 1))
				throw new Exception("bad highlighting colors table");
			return ret;
		}

		public static ModelColor? GetBackgroundColor(this FilterAction a)
		{
			if (a < FilterAction.IncludeAndColorizeFirst || a > FilterAction.IncludeAndColorizeLast)
				return null;
			return actionColors[a - FilterAction.IncludeAndColorizeFirst];
		}
	};
}
