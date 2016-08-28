namespace LogJoint
{
	internal class SearchObjectsFactory : ISearchObjectsFactory
	{
		readonly Progress.IProgressAggregatorFactory progressAggregatorFactory;
		readonly IInvokeSynchronization modelSynchronization;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly Telemetry.ITelemetryCollector telemetryCollector;

		public SearchObjectsFactory(
			Progress.IProgressAggregatorFactory progressAggregatorFactory,
			IInvokeSynchronization modelSynchronization,
			Settings.IGlobalSettingsAccessor settings,
			Telemetry.ITelemetryCollector telemetryCollector
		)
		{
			this.progressAggregatorFactory = progressAggregatorFactory;
			this.modelSynchronization = modelSynchronization;
			this.settings = settings;
			this.telemetryCollector = telemetryCollector;
		}

		ISearchResultInternal ISearchObjectsFactory.CreateSearchResults(
			ISearchManagerInternal owner, SearchAllOptions options, int id)
		{
			return new SearchResult(owner, options, progressAggregatorFactory, modelSynchronization, settings, id, this);
		}

		ISourceSearchResultInternal ISearchObjectsFactory.CreateSourceSearchResults(ILogSource source, ISearchResultInternal owner)
		{
			return new SourceSearchResult(source, owner, telemetryCollector);
		}
	};
}
