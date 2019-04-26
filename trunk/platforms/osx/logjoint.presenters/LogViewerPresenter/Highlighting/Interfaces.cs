using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal interface IHighlightingManager
	{
		IHighlightingHandler SearchResultHandler { get; }
		IHighlightingHandler HighlightingFiltersHandler { get; }
		IHighlightingHandler SelectionHandler { get; }
	};

	internal interface IHighlightingHandler
	{
		/// <summary>
		/// Enumerates ranges of Message's test that need highlighting. Only the ranges overlapping passed interval as enumerated.
		/// </summary>
		IEnumerable<(int, int, FilterAction)> GetHighlightingRanges(ViewLine vl);
	};
};