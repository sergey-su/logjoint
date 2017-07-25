namespace LogJoint
{
	public class FiltersManager :  IFiltersManager
	{
		readonly ILogSourcesManager logSources;
		readonly IFiltersList highlightFilters;
		readonly Settings.IGlobalSettingsAccessor globalSettings;

		public FiltersManager (
			IFiltersFactory filtersFactory,
			Settings.IGlobalSettingsAccessor globalSettingsAccessor,
			ILogSourcesManager logSourcesManager,
			IAdjustingColorsGenerator threadColors,
			IShutdown shutdown
		)
		{
			this.globalSettings = globalSettingsAccessor;
			this.logSources = logSourcesManager;

			this.highlightFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Highlighting);

			this.globalSettings.Changed += (sender, args) =>
			{
				if ((args.ChangedPieces & Settings.SettingsPiece.Appearance) != 0)
				{
					threadColors.Brightness = globalSettings.Appearance.ColoringBrightness;
				}
			};

			this.logSources.OnLogSourceRemoved += (s, e) =>
			{
				highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
			};

			shutdown.Cleanup += (sender, args) =>
			{
				highlightFilters.Dispose();
			};
		}

		IFiltersList IFiltersManager.HighlightFilters
		{
			get { return highlightFilters; }
		}
	}
}
