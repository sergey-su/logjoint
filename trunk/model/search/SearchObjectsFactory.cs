using System.Collections.Generic;
using System.Threading;

namespace LogJoint
{
	internal class SearchObjectsFactory : ISearchObjectsFactory
	{
		readonly Progress.IProgressAggregatorFactory progressAggregatorFactory;
		readonly ISynchronizationContext modelSynchronization;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly ITraceSourceFactory traceSourceFactory;

		public SearchObjectsFactory(
			Progress.IProgressAggregatorFactory progressAggregatorFactory,
			ISynchronizationContext modelSynchronization,
			Settings.IGlobalSettingsAccessor settings,
			Telemetry.ITelemetryCollector telemetryCollector,
			ITraceSourceFactory traceSourceFactory
		)
		{
			this.progressAggregatorFactory = progressAggregatorFactory;
			this.modelSynchronization = modelSynchronization;
			this.settings = settings;
			this.telemetryCollector = telemetryCollector;
			this.traceSourceFactory = traceSourceFactory;
		}

		ISearchResultInternal ISearchObjectsFactory.CreateSearchResults(
			ISearchManagerInternal owner, SearchAllOptions options, IFilter optionsFilter, int id, IList<ILogSourceSearchWorkerInternal> workers)
		{
			return new SearchResult(owner, options, optionsFilter, workers, progressAggregatorFactory, modelSynchronization, settings, id, this, traceSourceFactory);
		}

		ISourceSearchResultInternal ISearchObjectsFactory.CreateSourceSearchResults(
			ILogSourceSearchWorkerInternal searchWorker, 
			ISearchResultInternal owner,
			CancellationToken cancellation,
			Progress.IProgressAggregator progress
		)
		{
			return new SourceSearchResult(searchWorker, owner, cancellation, 
				progress, telemetryCollector);
		}

		ICombinedSearchResultInternal ISearchObjectsFactory.CreateCombinedSearchResult(ISearchManagerInternal owner)
		{
			return new CombinedSearchResult(owner, this);
		}

		ICombinedSourceSearchResultInternal ISearchObjectsFactory.CreateCombinedSourceSearchResult(ILogSource source)
		{
			return new CombinedSourceSearchResult(source);
		}

		ILogSourceSearchWorkerInternal ISearchObjectsFactory.CreateSearchWorker(ILogSource forSource, SearchAllOptions options)
		{
			return new SearchWorker(forSource, options, telemetryCollector);
		}
	};
}
