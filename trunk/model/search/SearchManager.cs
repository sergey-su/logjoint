using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public class SearchManager: ISearchManager, ISearchManagerInternal
	{
		readonly ILogSourcesManager sources;
		readonly ISearchObjectsFactory factory;
		readonly List<ISearchResultInternal> results = new List<ISearchResultInternal>();
		readonly AsyncInvokeHelper combinedResultUpdateInvoker;
		readonly LazyUpdateFlag combinedResultNeedsNotImmediateUpdateFlag;
		int lastId;
		ICombinedSearchResultInternal combinedSearchResult;
		Task combinedResultUpdater;
		CancellationTokenSource combinedResultUpdaterCancellation;

		public SearchManager(
			ILogSourcesManager sources, 
			Progress.IProgressAggregatorFactory progressAggregatorFactory, 
			IInvokeSynchronization modelSynchronization,
			Settings.IGlobalSettingsAccessor settings,
			Telemetry.ITelemetryCollector telemetryCollector,
			IHeartBeatTimer heartBeat
		) :this(
			sources,
			modelSynchronization, 
			heartBeat,
			new SearchObjectsFactory(progressAggregatorFactory, modelSynchronization, settings, telemetryCollector)
		)
		{
		}

		internal SearchManager(
			ILogSourcesManager sources,
			IInvokeSynchronization modelSynchronization,
			IHeartBeatTimer heartBeat,
			ISearchObjectsFactory factory
		)
		{
			this.sources = sources;
			this.factory = factory;

			this.combinedSearchResult = factory.CreateCombinedSearchResult(this);
			this.combinedResultUpdateInvoker = new AsyncInvokeHelper(
				modelSynchronization, (Action)UpdateCombinedResult);
			this.combinedResultNeedsNotImmediateUpdateFlag = new LazyUpdateFlag();

			sources.OnLogSourceAdded += (s, e) =>
			{
				results.ForEach(r => r.FireChangeEventIfContainsSourceResults(s as ILogSource));
			};
			sources.OnLogSourceRemoved += (s, e) =>
			{
				results.ForEach(r => r.FireChangeEventIfContainsSourceResults(s as ILogSource));

				// Search result is fully disposed if it contains messages
				// only from disposed log sources.
				// Fully disposed results are automatically dropped.
				var nrOfFullyDisposedResults = results.RemoveAll(
					r => r.Results.All(sr => sr.Source.IsDisposed));
				if (nrOfFullyDisposedResults > 0 && SearchResultsChanged != null)
					SearchResultsChanged(this, EventArgs.Empty);
			};
			heartBeat.OnTimer += (s, e) =>
			{
				if (e.IsNormalUpdate && combinedResultNeedsNotImmediateUpdateFlag.Validate())
					combinedResultUpdateInvoker.Invoke();
			};
		}

		public event EventHandler SearchResultsChanged;
		public event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;
		public event EventHandler CombinedSearchResultChanged;

		ISearchResult ISearchManager.SubmitSearch(SearchAllOptions options)
		{
			var currentTop = GetTopSearch();
			var result = factory.CreateSearchResults(this, options, ++lastId);
			results.ForEach(r => r.Cancel()); // cancel all active searches, cancelling of finished search has no effect
			results.Add(result);
			EnforceSearchesListLengthLimit();
			if (currentTop != null && !currentTop.Pinned)
				currentTop.Visible = false;
			result.StartSearch(sources);
			if (SearchResultsChanged != null)
				SearchResultsChanged(this, EventArgs.Empty);
			return result;
		}

		ICombinedSearchResult ISearchManager.CombinedSearchResult
		{
			get { return combinedSearchResult; }
		}

		IEnumerable<ISearchResult> ISearchManager.Results
		{
			get { return results; }
		}

		void ISearchManagerInternal.OnResultChanged(ISearchResult rslt, SearchResultChangeFlag flags)
		{
			if ((flags & SearchResultChangeFlag.StatusChanged) != 0
			 || (flags & SearchResultChangeFlag.VisibleChanged) != 0)
			{
				combinedResultUpdateInvoker.Invoke();
			}
			if ((flags & SearchResultChangeFlag.ResultsCollectionChanged) != 0
			  ||(flags & SearchResultChangeFlag.HitCountChanged) != 0)
			{
				combinedResultNeedsNotImmediateUpdateFlag.Invalidate();
			}
			if (SearchResultChanged != null)
			{
				SearchResultChanged(rslt, new SearchResultChangeEventArgs(flags));
			}
		}

		bool EnforceSearchesListLengthLimit()
		{
			int maxLengthOfSearchesHistory = 5; // todo: take from config
			var toBeDropped = 
				results
				.Where(r => !(r.Pinned || r.Id == lastId)) // find deletion candidates among not pinned results. last result is "pinned" indirectly.
				.OrderByDescending(r => r.HitsCount > 0 ? 1 : 0) // empty results are deleted first
				.ThenByDescending(r => r.Id) // oldest results deleted first
				.Skip(maxLengthOfSearchesHistory)
				.ToHashSet();
			return results.RemoveAll(r => toBeDropped.Contains(r)) > 0;
		}

		void UpdateCombinedResult()
		{
			combinedResultNeedsNotImmediateUpdateFlag.Validate();
			if (combinedResultUpdaterCancellation != null)
				combinedResultUpdaterCancellation.Cancel();
			var rslts = results.Where(r => r.Visible).SelectMany(r => r.Results.Where(
				sr => !sr.Source.IsDisposed && sr.Source.Visible)).ToArray();
			combinedResultUpdaterCancellation = new CancellationTokenSource();
			combinedResultUpdater = UpdateCombinedResultCore(rslts, combinedResultUpdaterCancellation.Token);
		}

		async Task UpdateCombinedResultCore(ISourceSearchResultInternal[] rslts, CancellationToken cancellation)
		{
			var newCombinedResult = factory.CreateCombinedSearchResult(this);
			newCombinedResult.Init(rslts, cancellation);
			Interlocked.Exchange(ref combinedSearchResult, newCombinedResult);
			var evt = CombinedSearchResultChanged;
			if (evt != null)
				evt(this, EventArgs.Empty);
		}

		ISearchResultInternal GetTopSearch()
		{
			ISearchResultInternal candidate =  null;
			foreach (var r in results)
				if (candidate == null || r.Id > candidate.Id)
					candidate = r;
			return candidate;
		}
	};
}