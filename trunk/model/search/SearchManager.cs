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
		readonly LazyUpdateFlag combinedResultNeedsLazyUpdateFlag;
		int lastId;
		ICombinedSearchResultInternal combinedSearchResult;
		Task combinedResultUpdater;
		CancellationTokenSource combinedResultUpdaterCancellation;

		public SearchManager(
			ILogSourcesManager sources, 
			Progress.IProgressAggregatorFactory progressAggregatorFactory, 
			ISynchronizationContext modelSynchronization,
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
			ISynchronizationContext modelSynchronization,
			IHeartBeatTimer heartBeat,
			ISearchObjectsFactory factory
		)
		{
			this.sources = sources;
			this.factory = factory;

			this.combinedSearchResult = factory.CreateCombinedSearchResult(this);
			this.combinedResultUpdateInvoker = new AsyncInvokeHelper(
				modelSynchronization, (Action)UpdateCombinedResult);
			this.combinedResultNeedsLazyUpdateFlag = new LazyUpdateFlag();

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
				var toBeDropped = results.Where(
					r => r.Results.All(sr => sr.Source.IsDisposed)).ToHashSet();
				var nrOfFullyDisposedResults = DisposeResults(toBeDropped);
				if (nrOfFullyDisposedResults > 0 && SearchResultsChanged != null)
					SearchResultsChanged(this, EventArgs.Empty);
				if (nrOfFullyDisposedResults > 0)
					combinedResultNeedsLazyUpdateFlag.Invalidate();
			};
			heartBeat.OnTimer += (s, e) =>
			{
				if (e.IsNormalUpdate && combinedResultNeedsLazyUpdateFlag.Validate())
					combinedResultUpdateInvoker.Invoke();
			};
		}

		public event EventHandler SearchResultsChanged;
		public event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;
		public event EventHandler CombinedSearchResultChanged;

		void ISearchManager.SubmitSearch(SearchAllOptions options)
		{
			var positiveFilters = options.Filters.GetPositiveFilters();
			if (positiveFilters.Count == 0)
				return;

			var searchWorkers = sources.Items.GetScopeSources(positiveFilters).Select(s => factory.CreateSearchWorker(s, options)).ToList();
			var newSearchResults = positiveFilters.Select(filter => factory.CreateSearchResults(this, options, filter, ++lastId, searchWorkers)).ToList();

			var currentTop = GetTopSearch();
			results.ForEach(r => r.Cancel()); // cancel all active searches, cancelling of finished searches has no effect
			RemoveSameOlderSearches(newSearchResults);
			results.AddRange(newSearchResults);
			EnforceSearchesListLengthLimit(lastId - newSearchResults.Count + 1);

			if (currentTop != null && !currentTop.Pinned)
				currentTop.Visible = false;

			searchWorkers.ForEach(w => w.Start());

			SearchResultsChanged?.Invoke(this, EventArgs.Empty);
		}

		ICombinedSearchResult ISearchManager.CombinedSearchResult
		{
			get { return combinedSearchResult; }
		}

		IEnumerable<ISearchResult> ISearchManager.Results
		{
			get { return results; }
		}

		void ISearchManager.Delete(ISearchResult rslt)
		{
			int? rsltIndex = results.IndexOf(r => r == rslt);
			if (rsltIndex == null)
				return;
			var rsltInternal = results[rsltIndex.Value];
			SearchResultsChanged?.Invoke (this, EventArgs.Empty);
			if (rsltInternal.HitsCount > 0)
				combinedResultNeedsLazyUpdateFlag.Invalidate();
			DisposeResults(new[] { rsltInternal }.ToHashSet());
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
				combinedResultNeedsLazyUpdateFlag.Invalidate();
			}
			SearchResultChanged?.Invoke (rslt, new SearchResultChangeEventArgs (flags));
		}

		bool EnforceSearchesListLengthLimit(int minFixedId)
		{
			int maxLengthOfSearchesHistory = 5; // todo: take from config
			var toBeDropped = 
				results
				.Where(r => !(r.Pinned || r.Id >= minFixedId)) // find deletion candidates among not pinned results
				.OrderByDescending(r => r.HitsCount > 0 ? 1 : 0) // empty results are deleted first
				.ThenByDescending(r => r.Id) // oldest results deleted first
				.Skip(maxLengthOfSearchesHistory)
				.ToHashSet();
			return DisposeResults(toBeDropped) > 0;
		}

		void RemoveSameOlderSearches(List<ISearchResultInternal> newSearches)
		{
			var newSearchResultsSet = new HashSet<ISearchResultInternal>(new SearchResultComparer());
			newSearches.ForEach(r => newSearchResultsSet.Add(r));
			var toBeDropped =
				results
				.Where(newSearchResultsSet.Contains)
				.ToHashSet();
			DisposeResults(toBeDropped);
		}

		void UpdateCombinedResult()
		{
			combinedResultNeedsLazyUpdateFlag.Validate();
			if (combinedResultUpdaterCancellation != null)
				combinedResultUpdaterCancellation.Cancel();
			var rslts = results.Where(r => r.Visible).SelectMany(r => r.Results.Where(
				sr => !sr.Source.IsDisposed && sr.Source.Visible)).ToArray();
			combinedResultUpdaterCancellation = new CancellationTokenSource();
			combinedResultUpdater = Task.Run(() => UpdateCombinedResultCore(rslts, combinedResultUpdaterCancellation.Token));
		}

		void UpdateCombinedResultCore(ISourceSearchResultInternal[] rslts, CancellationToken cancellation)
		{
			var newCombinedResult = factory.CreateCombinedSearchResult(this);
			newCombinedResult.Init(rslts, cancellation);
			if (cancellation.IsCancellationRequested)
				return;
			Interlocked.Exchange(ref combinedSearchResult, newCombinedResult);
			CombinedSearchResultChanged?.Invoke(this, EventArgs.Empty);
		}

		ISearchResultInternal GetTopSearch()
		{
			ISearchResultInternal candidate =  null;
			foreach (var r in results)
				if (candidate == null || r.Id > candidate.Id)
					candidate = r;
			return candidate;
		}

		int DisposeResults(HashSet<ISearchResultInternal> rslts)
		{
			foreach (var r in rslts)
				r.Dispose();
			return results.RemoveAll(rslts.Contains);
		}

		class SearchResultComparer : IEqualityComparer<ISearchResultInternal>
		{
			bool IEqualityComparer<ISearchResultInternal>.Equals(ISearchResultInternal x, ISearchResultInternal y)
			{
				if ((x.OptionsFilter != null) != (y.OptionsFilter != null))
					return false;
				if (x.OptionsFilter != null)
					return Search.Options.EqualityComparer.Equals(x.OptionsFilter.Options, y.OptionsFilter.Options);
				else
					return x.Options.SearchName == y.Options.SearchName;
			}

			int IEqualityComparer<ISearchResultInternal>.GetHashCode(ISearchResultInternal obj)
			{
				if (obj.OptionsFilter != null)
					return Search.Options.EqualityComparer.GetHashCode(obj.OptionsFilter.Options);
				else
					return obj.Options.SearchName?.GetHashCode() ?? 0;
			}
		}
	};
}