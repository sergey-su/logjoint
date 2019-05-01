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
			IWordSelection wordSelection,
			IColorTheme theme
		)
		{
			var viewSizeQuantizedSelector = Selectors.Create(
				viewSizeSelector,
				viewSize => (1 + (viewSize / 16)) * 16
			);
			this.getHighlightingHandler = Selectors.Create(
				() => (highlightFilters?.FilteringEnabled, highlightFilters?.Items, highlightFilters?.FiltersVersion),
				displayTextGetterSelector,
				viewSizeQuantizedSelector,
				() => theme.HighlightingColors,
				(filtersData, displayTextGetter, viewSize, hlColors) => filtersData.FilteringEnabled == true ? 
					  new CachingHighlightingHandler(msg => GetHlHighlightingRanges(msg, filtersData.Items, displayTextGetter, hlColors), ViewSizeToCacheSize(viewSize))
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
			foreach (var m in FindSearchMatches(msg, originalTextGetter, filters, skipWholeLines: false))
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

		private static IEnumerable<(int b, int e, ModelColor a)> FindSearchMatches(
			IMessage msg, MessageTextGetter textGetter, IFiltersList filters,
			bool skipWholeLines)
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
					if (skipWholeLines && r.WholeTextMatched)
						yield break;
					if (r.MatchBegin == r.MatchEnd)
						yield break;
					yield return (r.MatchBegin, r.MatchEnd, new ModelColor());
					startPos = r.MatchEnd;
				}
			}
		}

		private static int ViewSizeToCacheSize(int viewSize)
		{
			return Math.Max(viewSize, 1);
		}

		private static IEnumerable<(int, int, ModelColor)> GetHlHighlightingRanges(
			IMessage msg, ImmutableList<IFilter> hlFilters, MessageTextGetter displayTextGetter,
			ImmutableArray<ModelColor> hlColors)
		{
			var filtersState = hlFilters
				.Where(f => f.Enabled)
				.Select(filter => (filter.StartBulkProcessing(displayTextGetter, false), filter))
				.ToArray();

			for (int i = 0; i < filtersState.Length; ++i)
			{
				var filterState = filtersState[i];
				for (int? startPos = null; ;)
				{
					var rslt = filterState.Item1.Match(msg, startPos);
					if (rslt == null)
						break;
					var r = rslt.Value;
					if (r.MatchBegin == r.MatchEnd)
						break;
					yield return (r.MatchBegin, r.MatchEnd, filterState.filter.Action.ToColor(hlColors));
					if (r.WholeTextMatched)
						break;
					startPos = r.MatchEnd;
				}
			}

			foreach (var f in filtersState)
				f.Item1.Dispose();
		}

		private static IEnumerable<(int, int, ModelColor)> GetSearchResultsHighlightingRanges(
			IMessage msg, IFiltersList filters, MessageTextGetter displayTextGetter)
		{
			return FindSearchMatches(msg, displayTextGetter, filters, skipWholeLines: true);
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
				if (selectedPart.Any(c => !char.IsWhiteSpace(c)))
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

		private static IEnumerable<(int, int, ModelColor)> GetSelectionHighlightingRanges(
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
					yield return (r.MatchBegin, r.MatchEnd, new ModelColor());
				startPos = r.MatchEnd;
			}
		}


		private class DummyHandler : IHighlightingHandler
		{
			IEnumerable<(int, int, ModelColor)> IHighlightingHandler.GetHighlightingRanges(ViewLine vl)
			{
				yield break;
			}
		};
	};
};