using LogJoint.Progress;

namespace LogJoint
{
    public class FiltersManager : IFiltersManager
    {
        readonly ILogSourcesManager logSources;
        readonly IFiltersList highlightFilters;
        readonly IFiltersList displayFilters;
        readonly FilteringStats filteringStats;

        public FiltersManager(
            IFiltersFactory filtersFactory,
            ILogSourcesManager logSourcesManager,
            IShutdown shutdown,
            IChangeNotification changeNotification,
            IProgressAggregatorFactory progressAggregatorFactory
        )
        {
            this.logSources = logSourcesManager;
            this.filteringStats = new FilteringStats(progressAggregatorFactory);

            this.highlightFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Highlighting);
            this.displayFilters = filtersFactory.CreateFiltersList(FilterAction.Include, FiltersListPurpose.Display);

            this.logSources.OnLogSourceRemoved += (s, e) =>
            {
                highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
                displayFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
            };

            displayFilters.OnFiltersListChanged += (sender, evt) => logSources.Refresh();
            displayFilters.OnFilteringEnabledChanged += (sender, evt) => logSources.Refresh();
            displayFilters.OnPropertiesChanged += (sender, evt) => logSources.Refresh();

            shutdown.Cleanup += (sender, args) =>
            {
                highlightFilters.Dispose();
                displayFilters.Dispose();
            };
        }

        IFiltersList IFiltersManager.HighlightFilters => highlightFilters;

        IFiltersList IFiltersManager.DisplayFilters => displayFilters;

        FilteringStats IFiltersManager.FilteringStats => filteringStats;
    }
}
