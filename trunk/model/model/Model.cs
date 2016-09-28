using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using LogJoint.MRU;
using System.Threading.Tasks;

namespace LogJoint
{
	// todo: get rid of this class
	public class Model: 
		IModel
	{
		readonly ILogSourcesManager logSources;
		readonly IFiltersList highlightFilters;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly Persistence.IStorageManager storageManager;
		readonly Settings.IGlobalSettingsAccessor globalSettings;
		readonly ITempFilesManager tempFilesManager;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;

		public Model(
			ITempFilesManager tempFilesManager,
			IFiltersFactory filtersFactory,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			Persistence.IStorageManager storageManager,
			Settings.IGlobalSettingsAccessor globalSettingsAccessor,
			ILogSourcesManager logSourcesManager,
			IAdjustingColorsGenerator threadColors,
			IShutdown shutdown
		)
		{
			this.tempFilesManager = tempFilesManager;
			this.userDefinedFormatsManager = userDefinedFormatsManager;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.storageManager = storageManager;
			this.globalSettings = globalSettingsAccessor;
			this.logSources = logSourcesManager;
			this.highlightFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude);

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
				storageManager.Dispose();
			};
		}

		IFiltersList IModel.HighlightFilters
		{
			get { return highlightFilters; }
		}

		IUserDefinedFormatsManager IModel.UserDefinedFormatsManager
		{
			get { return userDefinedFormatsManager; }
		}

		ILogProviderFactoryRegistry IModel.LogProviderFactoryRegistry
		{
			get { return logProviderFactoryRegistry; }
		}

		ITempFilesManager IModel.TempFilesManager { get { return tempFilesManager; } }
	}
}
