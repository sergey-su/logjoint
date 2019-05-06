using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.Drawing;

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

		public static Color? ToColor(this FilterAction a, IReadOnlyList<Color> colors)
		{
			if (a >= FilterAction.IncludeAndColorizeFirst && a <= FilterAction.IncludeAndColorizeLast)
				return colors[a - FilterAction.IncludeAndColorizeFirst];
			return null;
		}
	};
}
