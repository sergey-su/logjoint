using LogJoint.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
    public static class FiltersExtensions
    {
        public static List<IFilter?> GetPositiveFilters(this IFiltersList filters)
        {
            var positiveFilters = filters.Items.Where(f => f.Enabled && f.Action != FilterAction.Exclude).ToList<IFilter?>();
            if (filters.GetDefaultAction() != FilterAction.Exclude)
                positiveFilters.Add(null);
            return positiveFilters;
        }

        public static IEnumerable<ILogSource> GetScopeSources(this IEnumerable<ILogSource> sources, List<IFilter?> positiveFilters)
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

        public static IEnumerable<(int beginIdx, int endIdx, FilterAction action)> GetHighlightRanges(
            this IReadOnlyList<IFilter> hlFilters, MessageTextGetter displayTextGetter, IMessage message)
        {
            return GetHighlightRanges(hlFilters, displayTextGetter,
                (processing, startPos) => processing.Match(message, startPos));
        }

        public static IEnumerable<(int beginIdx, int endIdx, FilterAction action)> GetHighlightRanges(
            this IReadOnlyList<IFilter> hlFilters, StringSlice text)
        {
            return GetHighlightRanges(hlFilters, MessageTextGetters.RawTextGetter /* unused */,
                (processing, startPos) => processing.Match(text, startPos));
        }

        private static IEnumerable<(int, int, FilterAction)> GetHighlightRanges(
            IReadOnlyList<IFilter> hlFilters, MessageTextGetter displayTextGetter, 
            Func<IFilterBulkProcessing, int?, Search.MatchedTextRange?> findNextRange)
        {
            var ret = new List<(int, int, FilterAction)>();
            var filtersState = hlFilters
                .Where(f => !f.IsDisposed && f.Enabled)
                .Select(filter => (procssing: filter.StartBulkProcessing(
                    displayTextGetter, false), filter))
                .ToArray();
            try
            {
                for (int i = 0; i < filtersState.Length; ++i)
                {
                    var (procssing, filter) = filtersState[i];
                    for (int? startPos = null; ;)
                    {
                        Search.MatchedTextRange? rslt = findNextRange(procssing, startPos);
                        if (rslt == null)
                            break;
                        var r = rslt.Value;
                        if (r.MatchBegin == r.MatchEnd)
                            break;
                        if (filter.Action == FilterAction.Exclude)
                            return [];
                        ret.Add((r.MatchBegin, r.MatchEnd, filter.Action));
                        if (r.WholeTextMatched)
                            break;
                        startPos = r.MatchEnd;
                    }
                }
                ret.Sort((a, b) => a.Item1 - b.Item1);
            }
            finally
            {
                foreach (var f in filtersState)
                    f.procssing.Dispose();
            }

            return ret;
        }
    };
}
