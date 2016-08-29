using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace LogJoint
{
	class SearchResult : ISearchResultInternal, ISearchResult
	{
		readonly ISearchObjectsFactory factory;
		readonly ISearchManagerInternal owner;
		readonly IInvokeSynchronization modelSynchronization;
		readonly SearchAllOptions options;
		readonly CancellationTokenSource cancellation;
		readonly List<ISourceSearchResultInternal> results;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly int hitsLimit;
		readonly AsyncInvokeHelper updateInvokationHelper;
		readonly int id;
		SearchResultStatus status; // accessed from model thread
		int hitsCounter; // accessed atomically from concurrent threads. it can become bigger than limit.
		bool visible;
		bool pinned;

		public SearchResult(
			ISearchManagerInternal owner,
			SearchAllOptions options,
			Progress.IProgressAggregatorFactory progressAggregatorFactory,
			IInvokeSynchronization modelSynchronization,
			Settings.IGlobalSettingsAccessor settings,
			int id,
			ISearchObjectsFactory factory
		)
		{
			this.owner = owner;
			this.options = options;
			this.factory = factory;
			this.modelSynchronization = modelSynchronization;
			this.id = id;
			this.cancellation = new CancellationTokenSource();
			this.results = new List<ISourceSearchResultInternal>();
			this.progressAggregator = progressAggregatorFactory.CreateProgressAggregator();
			this.updateInvokationHelper = new AsyncInvokeHelper(modelSynchronization, (Action)UpdateStatus);
			this.hitsLimit = settings.MaxNumberOfHitsInSearchResultsView;
			this.visible = true;

			this.progressAggregator.ProgressChanged += (s, e) =>
			{
				owner.OnResultChanged(this, SearchResultChangeFlag.ProgressChanged);
			};
		}

		int ISearchResult.Id
		{
			get { return id; }
		}

		SearchResultStatus ISearchResult.Status
		{
			get { return status; }
		}

		IEnumerable<ISourceSearchResult> ISearchResult.Results
		{
			get { return EnumVisibleResults(); }
		}

		SearchAllOptions ISearchResult.Options
		{
			get { return options; }
		}

		int ISearchResult.HitsCount
		{
			get { return EnumVisibleResults().Sum(r => r.HitsCount); }
		}

		double? ISearchResult.Progress
		{
			get { return progressAggregator.ProgressValue; }
		}

		bool ISearchResult.Visible
		{
			get { return visible; }
			set
			{
				visible = value;
				owner.OnResultChanged(this, SearchResultChangeFlag.VisibleChanged);
			}
		}

		bool ISearchResult.Pinned
		{
			get { return pinned; }
			set
			{
				pinned = value;
				owner.OnResultChanged(this, SearchResultChangeFlag.PinnedChanged);
			}
		}

		void ISearchResultInternal.StartSearch(ILogSourcesManager sources)
		{
			var sourcesResults = sources.Items.Select(
				src => factory.CreateSourceSearchResults(src, this)).ToList();
			results.AddRange(sourcesResults);
			sourcesResults.ForEach(r => r.StartTask(options, cancellation.Token, progressAggregator));
			if (results.Count == 0)
				status = SearchResultStatus.Finished;
		}

		void ISearchResult.Cancel()
		{
			cancellation.Cancel();
		}

		void ISearchResultInternal.OnResultChanged(ISourceSearchResultInternal rslt)
		{
			owner.OnResultChanged(this, SearchResultChangeFlag.HitCountChanged);
		}

		void ISearchResultInternal.OnResultCompleted(ISourceSearchResultInternal rslt)
		{
			updateInvokationHelper.Invoke();
		}

		bool ISearchResultInternal.AboutToAddNewMessage()
		{
			return Interlocked.Increment(ref hitsCounter) <= hitsLimit;
		}

		void ISearchResultInternal.FireChangeEventIfContainsSourceResults(ILogSource source)
		{
			if (results.Any(r => r.Source == source && r.HitsCount > 0))
			{
				owner.OnResultChanged(this, 
					SearchResultChangeFlag.ResultsCollectionChanged | SearchResultChangeFlag.HitCountChanged);
			}
		}

		IEnumerable<ISourceSearchResultInternal> ISearchResultInternal.Results
		{
			get { return results; }
		}

		void UpdateStatus()
		{
			if (status != SearchResultStatus.Active)
				return; // alredy in final state
			bool anyCancelled = false;
			bool anyReachedHitLimit = false;
			bool anyFailed = false;
			bool anyActive = false;
			bool allFinished = true;
			foreach (var r in results)
			{
				var rst = r.Status;
				if (rst == SearchResultStatus.Active)
					anyActive = true;
				if (rst == SearchResultStatus.Cancelled)
					anyCancelled = true;
				if (rst == SearchResultStatus.Failed)
					anyFailed = true;
				if (rst == SearchResultStatus.HitLimitReached)
					anyReachedHitLimit = true;
				if (rst != SearchResultStatus.Finished)
					allFinished = false;
			}
			if (anyActive)
				return;
			else if (anyFailed)
				status = SearchResultStatus.Failed;
			else if (anyCancelled)
				status = SearchResultStatus.Cancelled;
			else if (anyReachedHitLimit)
				status = SearchResultStatus.HitLimitReached;
			else if (allFinished)
				status = SearchResultStatus.Finished;
			else
				return;
			owner.OnResultChanged(this, SearchResultChangeFlag.StatusChanged);
		}

		IEnumerable<ISourceSearchResultInternal> EnumVisibleResults()
		{
			return results.Where(r => !r.Source.IsDisposed && r.Source.Visible);
		}
	};
}
