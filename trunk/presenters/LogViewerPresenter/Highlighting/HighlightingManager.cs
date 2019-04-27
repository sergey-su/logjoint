using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal class HighlightingManager : IHighlightingManager
	{
		readonly Func<IHighlightingHandler> getHighlightingHandler;
		readonly Func<IHighlightingHandler> getSearchResultHandler;
		readonly Func<IHighlightingHandler> getSelectionHandler;

		public HighlightingManager(
			ISearchResultModel searchResultModel,
			Func<MessageTextGetter> displayTextGetterSelector,
			Func<int> viewSizeSelector,
			IFiltersList highlightFilters,
			ISelectionManager selectionManager,
			IWordSelection wordSelection
		)
		{
			var viewSizeQuantizedSelector = Selectors.Create(
				viewSizeSelector,
				viewSize => (1 + (viewSize / 16)) * 16
			);
			this.getHighlightingHandler = Selectors.Create(
				() => highlightFilters?.FilteringEnabled,
				() => highlightFilters?.Items,
				displayTextGetterSelector,
				() => highlightFilters?.FiltersVersion,
				viewSizeQuantizedSelector,
				(filteringEnabled, filters, displayTextGetter, _, viewSize) => filteringEnabled == true ? 
					  new CachingHighlightingHandler(msg => GetHlHighlightingRanges(msg, filters, displayTextGetter), ViewSizeToCacheSize(viewSize))
					: (IHighlightingHandler)new DummyHandler()
			);
			this.getSearchResultHandler = Selectors.Create(
				() => searchResultModel?.SearchFiltersList,
				displayTextGetterSelector,
				viewSizeQuantizedSelector,
				(filters, displayTextGetter, viewSize) => filters != null ?
					  new CachingHighlightingHandler(msg => GetSearchResultsHighlightingRanges(msg, filters, displayTextGetter), ViewSizeToCacheSize(viewSize))
					: null
			);
			this.getSelectionHandler = Selectors.Create(
				() => selectionManager.Selection,
				displayTextGetterSelector,
				viewSizeQuantizedSelector,
				(selection, displayTextGetter, viewSize) =>
					MakeSelectionInplaceHighlightingHander(selection, displayTextGetter, wordSelection, ViewSizeToCacheSize(viewSize))
			);
		}

		IHighlightingHandler IHighlightingManager.SearchResultHandler => getSearchResultHandler();

		IHighlightingHandler IHighlightingManager.HighlightingFiltersHandler => getHighlightingHandler();

		IHighlightingHandler IHighlightingManager.SelectionHandler => getSelectionHandler();

		MessageDisplayTextInfo IHighlightingManager.GetSearchResultMessageText(IMessage msg, MessageTextGetter originalTextGetter, IFiltersList filters)
		{
			var originalText = originalTextGetter(msg);
			var retLines = new List<StringSlice>();
			var retLinesMap = new List<int>();
			int originalTextLineIdx = 0;
			foreach (var m in FindSearchMatches(msg, originalTextGetter, filters))
			{
				for (int stage = 0; originalTextLineIdx < originalText.GetLinesCount() && stage != 2; ++originalTextLineIdx)
				{
					var line = originalText.GetNthTextLine(originalTextLineIdx);
					var lineBeginIndex = line.StartIndex - originalText.Text.StartIndex;
					var lineEndIndex = line.EndIndex - originalText.Text.StartIndex;
					if (lineBeginIndex >= m.e)
						break;
					if (stage == 0 && lineEndIndex >= m.b)
						stage = 1;
					if (stage == 1 && lineEndIndex >= m.e)
						stage = 2;
					if (stage != 0)
					{
						retLines.Add(line);
						retLinesMap.Add(originalTextLineIdx);
					}
				}
			}
			if (originalTextLineIdx == 0)
			{
				retLines.Add(originalText.GetNthTextLine(0));
				retLinesMap.Add(originalTextLineIdx);
			}
			return new MessageDisplayTextInfo()
			{
				DisplayText = new StringUtils.MultilineText(new StringSlice(string.Join("\n", retLines))),
				LinesMapper = i => retLinesMap.ElementAtOrDefault(i),
				ReverseLinesMapper = i =>
				{
					int result = 0;
					while (result < retLinesMap.Count && retLinesMap[result] < i)
						++result;
					return result;
				}
			};
		}

		private static IEnumerable<(int b, int e)> FindSearchMatches(IMessage msg, MessageTextGetter textGetter, IFiltersList filters) // todo: merge with GetSearchResultsHighlightingRanges
		{
			IFiltersListBulkProcessing processing;
			try
			{
				processing = filters.StartBulkProcessing(textGetter, reverseMatchDirection: false);
			}
			catch (Search.TemplateException)
			{
				yield break;
			}
			using (processing)
			{
				for (int? startPos = null; ;)
				{
					var rslt = processing.ProcessMessage(msg, startPos);
					if (rslt.Action == FilterAction.Exclude || rslt.MatchedRange == null)
						yield break;
					var r = rslt.MatchedRange.Value;
					if (r.MatchBegin == r.MatchEnd)
						yield break;
					yield return (r.MatchBegin, r.MatchEnd);
					startPos = r.MatchEnd;
				}
			}
		}

		private static int ViewSizeToCacheSize(int viewSize)
		{
			return Math.Max(viewSize, 1);
		}

		private static IEnumerable<(int, int, FilterAction)> GetHlHighlightingRanges(
			IMessage msg, ImmutableList<IFilter> hlFilters, MessageTextGetter displayTextGetter)
		{
			var filtersState = hlFilters
				.Where(f => f.Enabled)
				.Select(f => (f.StartBulkProcessing(displayTextGetter, false), f))
				.ToArray();

			for (int i = 0; i < filtersState.Length; ++i)
			{
				var f = filtersState[i];
				for (int? startPos = null; ;)
				{
					var rslt = f.Item1.Match(msg, startPos);
					if (rslt == null)
						break;
					var r = rslt.Value;
					if (r.MatchBegin == r.MatchEnd)
						break;
					yield return (r.MatchBegin, r.MatchEnd, f.Item2.Action);
					if (r.WholeTextMatched)
						break;
					startPos = r.MatchEnd;
				}
			}

			foreach (var f in filtersState)
				f.Item1.Dispose();
		}

		private static IEnumerable<(int, int, FilterAction)> GetSearchResultsHighlightingRanges(
			IMessage msg, IFiltersList filters, MessageTextGetter displayTextGetter)
		{
			IFiltersListBulkProcessing processing;
			try
			{
				processing = filters.StartBulkProcessing(displayTextGetter, reverseMatchDirection: false);
			}
			catch (Search.TemplateException)
			{
				yield break;
			}
			using (processing)
			{
				for (int? startPos = null; ;)
				{
					var rslt = processing.ProcessMessage(msg, startPos);
					if (rslt.Action == FilterAction.Exclude || rslt.MatchedRange == null)
						yield break;
					var r = rslt.MatchedRange.Value;
					if (r.WholeTextMatched)
						yield break;
					if (r.MatchBegin == r.MatchEnd)
						yield break;
					yield return (r.MatchBegin, r.MatchEnd, rslt.Action);
					startPos = r.MatchEnd;
				}
			}
		}

		private static IHighlightingHandler MakeSelectionInplaceHighlightingHander(
			SelectionInfo selection, MessageTextGetter displayTextGetter, IWordSelection wordSelection, int cacheSize)
		{
			IHighlightingHandler newHandler = null;

			if (selection?.IsSingleLine == true)
			{
				var normSelection = selection.Normalize();
				var text = displayTextGetter(normSelection.First.Message);
				var line = text.GetNthTextLine(normSelection.First.TextLineIndex);
				int beginIdx = normSelection.First.LineCharIndex;
				int endIdx = normSelection.Last.LineCharIndex;
				var selectedPart = line.SubString(beginIdx, endIdx - beginIdx);
				if (!selectedPart.IsEmpty)
				{
					var options = new Search.Options() 
					{
						Template = selectedPart,
						MessageTextGetter = displayTextGetter,
					};
					var optionsPreprocessed = options.BeginSearch();
					newHandler = new CachingHighlightingHandler(msg => GetSelectionHighlightingRanges(msg, optionsPreprocessed, wordSelection, 
						(normSelection.First.Message, beginIdx + line.StartIndex - text.Text.StartIndex)), cacheSize);
				}
			}

			return newHandler;
		}

		private static IEnumerable<(int, int, FilterAction)> GetSelectionHighlightingRanges(
			IMessage msg, Search.SearchState searchOpts, IWordSelection wordSelection, (IMessage msg, int charIdx) originalSelection)
		{
			for (int? startPos = null; ;)
			{
				var matchedTextRangle = Search.SearchInMessageText(msg, searchOpts, startPos);
				if (!matchedTextRangle.HasValue)
					yield break;
				var r = matchedTextRangle.Value;
				if (r.WholeTextMatched)
					yield break;
				if (r.MatchBegin == r.MatchEnd)
					yield break;
				if (!(msg == originalSelection.msg && r.MatchBegin == originalSelection.charIdx))
					yield return (r.MatchBegin, r.MatchEnd, FilterAction.Include);
				startPos = r.MatchEnd;
			}
		}


		private class DummyHandler : IHighlightingHandler
		{
			IEnumerable<(int, int, FilterAction)> IHighlightingHandler.GetHighlightingRanges(ViewLine vl)
			{
				yield break;
			}
		};
	};
};