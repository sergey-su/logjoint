using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal interface IHighlightingManager
	{
		IHighlightingHandler CreateSearchResultHandler();
		IHighlightingHandler CreateHighlightingFiltersHandler();
	};


	internal class HighlightingManager : IHighlightingManager
	{
		readonly ISearchResultModel searchResultModel;
		readonly IPresentationDataAccess presentationDataAccess;
		readonly IFiltersList highlightFilters;

		public HighlightingManager(
			ISearchResultModel searchResultModel,
			IPresentationDataAccess presentationDataAccess,
			IFiltersList highlightFilters
		)
		{
			this.searchResultModel = searchResultModel;
			this.presentationDataAccess = presentationDataAccess;
			this.highlightFilters = highlightFilters;
		}


		IHighlightingHandler IHighlightingManager.CreateSearchResultHandler()
		{
			if (searchResultModel == null)
				return null;
			try
			{
				return new SearchResultHandler(searchResultModel.CreateSearchFiltersList(), presentationDataAccess.ShowRawMessages);
			}
			catch (Search.TemplateException)
			{
				return null;
			}
		}

		IHighlightingHandler IHighlightingManager.CreateHighlightingFiltersHandler()
		{
			if (highlightFilters == null)
				return null;
			if (!highlightFilters.FilteringEnabled)
				return new DummyHandler();
			return new HighlightFiltersHandler(highlightFilters, presentationDataAccess.ShowRawMessages);
		}

		class SearchResultHandler : IHighlightingHandler
		{
			readonly IFiltersList filter;
			readonly IFiltersListBulkProcessing processing;

			public SearchResultHandler(IFiltersList filters, bool matchRawMessages)
			{
				this.filter = filters;
				this.processing = filters.StartBulkProcessing(matchRawMessages, reverseMatchDirection: false);
			}

			public void Dispose()
			{
				processing.Dispose();
				filter.Dispose();
			}

			IEnumerable<Tuple<int, int, FilterAction>> IHighlightingHandler.GetHighlightingRanges(IMessage msg)
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
					yield return Tuple.Create(r.MatchBegin, r.MatchEnd, rslt.Action);
					startPos = r.MatchEnd;
				}
			}
		};

		class DummyHandler : IHighlightingHandler
		{
			void IDisposable.Dispose()
			{
			}

			IEnumerable<Tuple<int, int, FilterAction>> IHighlightingHandler.GetHighlightingRanges(IMessage msg)
			{
				yield break;
			}
		};

		class HighlightFiltersHandler : IHighlightingHandler
		{
			readonly KeyValuePair<IFilterBulkProcessing, IFilter>[] filters;

			public HighlightFiltersHandler(IFiltersList filters, bool matchRawMessages)
			{
				this.filters = filters
					.Items
					.Where(f => f.Enabled)
					.Select(f => new KeyValuePair<IFilterBulkProcessing, IFilter>(
						f.StartBulkProcessing(matchRawMessages, false), f
					))
					.ToArray();
			}

			public void Dispose()
			{
				foreach (var f in filters)
					f.Key.Dispose();
			}

			IEnumerable<Tuple<int, int, FilterAction>> IHighlightingHandler.GetHighlightingRanges(IMessage msg)
			{
				for (int i = 0; i < filters.Length; ++i)
				{
					var f = filters[i];
					for (int? startPos = null; ;)
					{
						var rslt = f.Key.Match(msg, startPos);
						if (rslt == null)
							break;
						var r = rslt.Value;
						if (r.MatchBegin == r.MatchEnd)
							break;
						yield return Tuple.Create(r.MatchBegin, r.MatchEnd, f.Value.Action);
						if (r.WholeTextMatched)
							break;
						startPos = r.MatchEnd;
					}
				}
			}
		};
	};
};