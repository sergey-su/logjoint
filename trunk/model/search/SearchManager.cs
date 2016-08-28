using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint
{
	public class SearchManager: ISearchManager, ISearchManagerInternal
	{
		readonly ILogSourcesManager sources;
		readonly ISearchObjectsFactory factory;
		readonly List<ISearchResultInternal> results = new List<ISearchResultInternal>();
		int lastId;

		public SearchManager(
			ILogSourcesManager sources, 
			Progress.IProgressAggregatorFactory progressAggregatorFactory, 
			IInvokeSynchronization modelSynchronization,
			Settings.IGlobalSettingsAccessor settings,
			Telemetry.ITelemetryCollector telemetryCollector
		) :
			this(sources, new SearchObjectsFactory(progressAggregatorFactory, modelSynchronization, settings, telemetryCollector))
		{
		}

		internal SearchManager(
			ILogSourcesManager sources,
			ISearchObjectsFactory factory
		)
		{
			this.sources = sources;
			this.factory = factory;

			sources.OnLogSourceAdded += (s, e) =>
			{
				results.ForEach(r => r.FireChangeEventIfContainsSourceResults(s as ILogSource));
			};
			sources.OnLogSourceRemoved += (s, e) =>
			{
				results.ForEach(r => r.FireChangeEventIfContainsSourceResults(s as ILogSource));
			};
		}

		public event EventHandler SearchResultsChanged;
		public event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;

		ISearchResult ISearchManager.SubmitSearch(SearchAllOptions options)
		{
			var result = factory.CreateSearchResults(this, options, ++lastId);
			result.StartSearch(sources);
			results.ForEach(r => r.Cancel()); // cancel all active searches, cancelling of finished search has no effect
			results.Add(result);
			EnforceSearchesListLengthLimit();
			if (SearchResultsChanged != null)
				SearchResultsChanged(this, EventArgs.Empty);
			return result;
		}

		IEnumerable<ISearchResult> ISearchManager.Results
		{
			get { return results; }
		}

		void ISearchManagerInternal.OnResultChanged(ISearchResult rslt, SearchResultChangeFlag flags)
		{
			if (SearchResultChanged != null)
			{
				SearchResultChanged(rslt, new SearchResultChangeEventArgs(flags));
			}
		}

		bool EnforceSearchesListLengthLimit()
		{
			int maxLengthOfSearchesHistory = 3; // todo: take from config
			var toBeDropped = results.Where(r => !r.Pinned).OrderByDescending(r => r.Id).Skip(maxLengthOfSearchesHistory).ToHashSet();
			return results.RemoveAll(r => toBeDropped.Contains(r)) > 0;
		}
	};
}