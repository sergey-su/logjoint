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

		public static ModelColor ToColor(this FilterAction a, IReadOnlyList<ModelColor> colors)
		{
			return colors[a - FilterAction.IncludeAndColorizeFirst];
		}
	};
}
