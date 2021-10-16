using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace LogJoint.Settings
{
	public class GlobalSettingsAccessor : IGlobalSettingsAccessor
	{
		readonly Persistence.IStorageManager storageManager;
		const string sectionName = "settings";
		const string rootNodeName = "root";
		const string fullLoadingSizeThresholdAttrName = "full-loading-size-threshold";
		const string logWindowsSizeAttrName = "log-window-size";
		const string maxSearchResultSizeAttrName = "max-search-result-size";
		const string multithreadedParsingDisabledAttrName = "multithreaded-parsing-disabled";
		const string userDataStoreSizeLimitAttrName = "store-size-limit";
		const string userDataStoreCleanupPeriodAttrName = "store-cleanup-period";
		const string contentCacheSizeLimitAttrName = "content-cache-size-limit";
		const string contentCacheCleanupPeriodAttrName = "content-cache-cleanup-period";
		const string enableAutoPostprocessingAttrName = "enable-auto-postprocessing";

		const string fontSizeAttrName = "font-size";
		const string fontNameAttrName = "font-name";
		const string coloringAttrName = "coloring";
		const string coloringPaletteAttrName = "coloring-palette";

		const int MaxMaxSearchResultSize = DefaultSettingsAccessor.DefaultMaxSearchResultSize * 100;

		bool loaded;
		FileSizes fileSizes = FileSizes.Default;
		int maxNumberOfHitsInSearchResultsView = DefaultSettingsAccessor.DefaultMaxSearchResultSize;
		bool multithreadedParsingDisabled = DefaultSettingsAccessor.DefaultMultithreadedParsingDisabled;
		Appearance appearance = Appearance.Default;
		StorageSizes userDataStorageSizes = StorageSizes.Default;
		StorageSizes contentCacheStorageSizes = StorageSizes.Default;
		bool enableAutoPostprocessing = DefaultSettingsAccessor.DefaultEnableAutoPostprocessing;

		TaskChain tasks = new TaskChain();

		public GlobalSettingsAccessor(Persistence.IStorageManager storageManager)
		{
			this.storageManager = storageManager;
			tasks.AddTask(Load);
		}

		public event EventHandler<SettingsChangeEvent> Changed;

		FileSizes IGlobalSettingsAccessor.FileSizes
		{
			get
			{
				return fileSizes;
			}
			set
			{
				Validate(ref value);
				tasks.AddTask(async () =>
				{
					if (loaded && !Differ(value, fileSizes))
						return;
					fileSizes = value;
					FireChanged(SettingsPiece.FileSizes);
					await Save();
				});
			}
		}

		int IGlobalSettingsAccessor.MaxNumberOfHitsInSearchResultsView
		{
			get
			{
				return maxNumberOfHitsInSearchResultsView;
			}
			set
			{
				value = RangeUtils.PutInRange(1, MaxMaxSearchResultSize, value);
				tasks.AddTask(async () =>
				{
					if (loaded && value == maxNumberOfHitsInSearchResultsView)
						return;
					maxNumberOfHitsInSearchResultsView = value;
					await Save();
				});
			}
		}

		bool IGlobalSettingsAccessor.MultithreadedParsingDisabled
		{
			get
			{
				return multithreadedParsingDisabled;
			}
			set
			{
				tasks.AddTask(async () =>
				{
					if (loaded && value == multithreadedParsingDisabled)
						return;
					multithreadedParsingDisabled = value;
					await Save();
				});
			}
		}

		Appearance IGlobalSettingsAccessor.Appearance
		{
			get
			{
				return appearance;
			}
			set
			{
				Validate(ref value);
				tasks.AddTask(async () =>
				{
					if (loaded && !Differ(value, appearance))
						return;
					appearance = value;
					FireChanged(SettingsPiece.Appearance);
					await Save();
				});
			}
		}

		StorageSizes IGlobalSettingsAccessor.UserDataStorageSizes
		{
			get
			{
				return userDataStorageSizes;
			}
			set
			{
				Validate(ref value);
				tasks.AddTask(async () =>
				{
					if (loaded && !Differ(value, userDataStorageSizes))
						return;
					userDataStorageSizes = value;
					FireChanged(SettingsPiece.UserDataStorageSizes);
					await Save();
				});
			}
		}

		StorageSizes IGlobalSettingsAccessor.ContentStorageSizes
		{
			get
			{
				return contentCacheStorageSizes;
			}
			set
			{
				Validate(ref value);
				tasks.AddTask(async () =>
				{
					if (loaded && !Differ(value, contentCacheStorageSizes))
						return;
					contentCacheStorageSizes = value;
					FireChanged(SettingsPiece.ContentCacheStorageSizes);
					await Save();
				});
			}
		}

		bool IGlobalSettingsAccessor.EnableAutoPostprocessing
		{
			get
			{
				return enableAutoPostprocessing;
			}
			set
			{
				tasks.AddTask(async () =>
				{
					if (loaded && value == enableAutoPostprocessing)
						return;
					enableAutoPostprocessing = value;
					await Save();
				});
			}
		}


		private async Task Load()
		{
            await using (var section = await (await storageManager.GlobalSettingsEntry).OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var root = section.Data.Element(rootNodeName);

				fileSizes.Threshold = root.SafeIntValue(fullLoadingSizeThresholdAttrName, FileSizes.Default.Threshold);
				fileSizes.WindowSize = root.SafeIntValue(logWindowsSizeAttrName, FileSizes.Default.WindowSize);
				Validate(ref fileSizes);

				maxNumberOfHitsInSearchResultsView = root.SafeIntValue(maxSearchResultSizeAttrName, DefaultSettingsAccessor.DefaultMaxSearchResultSize);

				multithreadedParsingDisabled = root.SafeIntValue(multithreadedParsingDisabledAttrName, DefaultSettingsAccessor.DefaultMultithreadedParsingDisabled ? 1 : 0) != 0;

				appearance = new Appearance(
					fontSize: (Appearance.LogFontSize)root.SafeIntValue(fontSizeAttrName, (int)Appearance.Default.FontSize),
					fontFamily: root.AttributeValue(fontNameAttrName, Appearance.Default.FontFamily),
					coloring: (Appearance.ColoringMode)root.SafeIntValue(coloringAttrName, (int)Appearance.Default.Coloring),
					coloringBrightness: (PaletteBrightness)root.SafeIntValue(coloringPaletteAttrName, (int)Appearance.Default.ColoringBrightness)
				);
				Validate(ref appearance);

				userDataStorageSizes.StoreSizeLimit = root.SafeIntValue(userDataStoreSizeLimitAttrName, StorageSizes.Default.StoreSizeLimit);
				userDataStorageSizes.CleanupPeriod = root.SafeIntValue(userDataStoreCleanupPeriodAttrName, StorageSizes.Default.CleanupPeriod);
				Validate(ref userDataStorageSizes);

				contentCacheStorageSizes.StoreSizeLimit = root.SafeIntValue(contentCacheSizeLimitAttrName, StorageSizes.Default.StoreSizeLimit);
				contentCacheStorageSizes.CleanupPeriod = root.SafeIntValue(contentCacheCleanupPeriodAttrName, StorageSizes.Default.CleanupPeriod);
				Validate(ref contentCacheStorageSizes);

				enableAutoPostprocessing = root.SafeIntValue(enableAutoPostprocessingAttrName, DefaultSettingsAccessor.DefaultEnableAutoPostprocessing ? 1 : 0) != 0;
			}
			loaded = true;
		}

		async Task Save()
		{
            await using var section = await (await storageManager.GlobalSettingsEntry).OpenXMLSection(sectionName,
                Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen);
            var root = new XElement(
                rootNodeName,
                new XAttribute(fullLoadingSizeThresholdAttrName, fileSizes.Threshold),
                new XAttribute(logWindowsSizeAttrName, fileSizes.WindowSize),
                new XAttribute(maxSearchResultSizeAttrName, maxNumberOfHitsInSearchResultsView),
                new XAttribute(multithreadedParsingDisabledAttrName, multithreadedParsingDisabled ? "1" : "0"),
                new XAttribute(fontSizeAttrName, (int)appearance.FontSize),
                new XAttribute(coloringAttrName, (int)appearance.Coloring),
                new XAttribute(coloringPaletteAttrName, (int)appearance.ColoringBrightness),
                new XAttribute(userDataStoreSizeLimitAttrName, userDataStorageSizes.StoreSizeLimit),
                new XAttribute(userDataStoreCleanupPeriodAttrName, userDataStorageSizes.CleanupPeriod),
                new XAttribute(contentCacheSizeLimitAttrName, contentCacheStorageSizes.StoreSizeLimit),
                new XAttribute(contentCacheCleanupPeriodAttrName, contentCacheStorageSizes.CleanupPeriod),
                new XAttribute(enableAutoPostprocessingAttrName, enableAutoPostprocessing ? "1" : "0")
            );
            if (appearance.FontFamily != null)
                root.Add(new XAttribute(fontNameAttrName, appearance.FontFamily));
            section.Data.Add(root);
        }

		static void Validate(ref FileSizes fileSizes)
		{
			fileSizes.WindowSize = RangeUtils.PutInRange(FileSizes.MinWindowSize, FileSizes.MaxWindowSize, fileSizes.WindowSize);
			fileSizes.Threshold = RangeUtils.PutInRange(fileSizes.WindowSize, FileSizes.MaxThreshold, fileSizes.Threshold);
		}

		static void Validate(ref Appearance appearance)
		{
			appearance.Coloring = (Appearance.ColoringMode)RangeUtils.PutInRange(
				(int)Appearance.ColoringMode.Minimum, (int)Appearance.ColoringMode.Maximum, (int)appearance.Coloring);
			appearance.FontSize = (Appearance.LogFontSize)RangeUtils.PutInRange(
				(int)Appearance.LogFontSize.Minimum, (int)Appearance.LogFontSize.Maximum, (int)appearance.FontSize);
			appearance.ColoringBrightness = (PaletteBrightness)RangeUtils.PutInRange(
				(int)PaletteBrightness.Minimum, (int)PaletteBrightness.Maximum, (int)appearance.ColoringBrightness);
		}

		static void Validate(ref StorageSizes storageSizes)
		{
			storageSizes.StoreSizeLimit = RangeUtils.PutInRange(StorageSizes.MinStoreSizeLimit, int.MaxValue, storageSizes.StoreSizeLimit);
			storageSizes.CleanupPeriod = RangeUtils.PutInRange(StorageSizes.MinCleanupPeriod, StorageSizes.MaxCleanupPeriod, storageSizes.CleanupPeriod);
		}

		static bool Differ(FileSizes val1, FileSizes val2)
		{
			return val1.Threshold != val2.Threshold || val1.WindowSize != val2.WindowSize;
		}

		static bool Differ(Appearance val1, Appearance val2)
		{
			return
				val1.FontSize != val2.FontSize || 
				val1.FontFamily != val2.FontFamily || 
				val1.Coloring != val2.Coloring ||
				val1.ColoringBrightness != val2.ColoringBrightness;
		}

		static bool Differ(StorageSizes val1, StorageSizes val2)
		{
			return
				val1.StoreSizeLimit != val2.StoreSizeLimit ||
				val1.CleanupPeriod != val2.CleanupPeriod;
		}

		void FireChanged(SettingsPiece settingsPiece)
		{
			Changed?.Invoke(this, new SettingsChangeEvent(settingsPiece));
		}
	}
}
