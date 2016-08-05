using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class SearchManager: ISearchManager, ISearchManagerInternal
	{
		readonly ILogSourcesManager sources;
		readonly ISearchObjectsFactory factory;
		readonly List<ISearchResultInternal> results = new List<ISearchResultInternal>();

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

		ISearchResult ISearchManager.SubmitSearch(Search.Options options)
		{
			var result = factory.CreateSearchResults(this, options);
			result.StartSearch(sources);
			results.ForEach(r => r.Cancel()); // cancel all active searches
			results.RemoveAll(r => !r.Pinned);
			results.Add(result);
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
	};
}
