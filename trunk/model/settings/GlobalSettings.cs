using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System;

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
		FileSizes fileSizes;
		int maxNumberOfHitsInSearchResultsView;
		bool multithreadedParsingDisabled;
		Appearance appearance;
		StorageSizes userDataStorageSizes, contentCacheStorageSizes;
		bool enableAutoPostprocessing;

		public GlobalSettingsAccessor(Persistence.IStorageManager persistenceEntry)
		{
			this.storageManager = persistenceEntry;
		}

		public event EventHandler<SettingsChangeEvent> Changed;

		FileSizes IGlobalSettingsAccessor.FileSizes
		{
			get
			{
				EnsureLoaded();
				return fileSizes;
			}
			set
			{
				Validate(ref value);
				if (loaded && !Differ(value, fileSizes))
					return;
				EnsureLoaded();
				fileSizes = value;
				Save();
				FireChanged(SettingsPiece.FileSizes);
			}
		}

		int IGlobalSettingsAccessor.MaxNumberOfHitsInSearchResultsView
		{
			get
			{
				EnsureLoaded();
				return maxNumberOfHitsInSearchResultsView;
			}
			set
			{
				value = RangeUtils.PutInRange(1, MaxMaxSearchResultSize, value);
				if (loaded && value == maxNumberOfHitsInSearchResultsView)
					return;
				EnsureLoaded();
				maxNumberOfHitsInSearchResultsView = value;
				Save();
			}
		}

		bool IGlobalSettingsAccessor.MultithreadedParsingDisabled
		{
			get
			{
				EnsureLoaded();
				return multithreadedParsingDisabled;
			}
			set
			{
				if (loaded && value == multithreadedParsingDisabled)
					return;
				EnsureLoaded();
				multithreadedParsingDisabled = value;
				Save();
			}
		}

		Appearance IGlobalSettingsAccessor.Appearance
		{
			get
			{
				EnsureLoaded();
				return appearance;
			}
			set
			{
				Validate(ref value);
				if (loaded && !Differ(value, appearance))
					return;
				EnsureLoaded();
				appearance = value;
				Save();
				FireChanged(SettingsPiece.Appearance);
			}
		}

		StorageSizes IGlobalSettingsAccessor.UserDataStorageSizes
		{
			get
			{
				EnsureLoaded();
				return userDataStorageSizes;
			}
			set
			{
				Validate(ref value);
				if (loaded && !Differ(value, userDataStorageSizes))
					return;
				EnsureLoaded();
				userDataStorageSizes = value;
				Save();
				FireChanged(SettingsPiece.UserDataStorageSizes);
			}
		}

		StorageSizes IGlobalSettingsAccessor.ContentStorageSizes
		{
			get
			{
				EnsureLoaded();
				return contentCacheStorageSizes;
			}
			set
			{
				Validate(ref value);
				if (loaded && !Differ(value, contentCacheStorageSizes))
					return;
				EnsureLoaded();
				contentCacheStorageSizes = value;
				Save();
				FireChanged(SettingsPiece.ContentCacheStorageSizes);
			}
		}

		bool IGlobalSettingsAccessor.EnableAutoPostprocessing
		{
			get
			{
				EnsureLoaded();
				return enableAutoPostprocessing;
			}
			set
			{
				if (loaded && value == enableAutoPostprocessing)
					return;
				EnsureLoaded();
				enableAutoPostprocessing = value;
				Save();
			}
		}


		void EnsureLoaded()
		{
			if (loaded)
				return;
			using (var section = storageManager.GlobalSettingsEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var root = section.Data.Element(rootNodeName);

				fileSizes.Threshold = root.SafeIntValue(fullLoadingSizeThresholdAttrName, FileSizes.Default.Threshold);
				fileSizes.WindowSize = root.SafeIntValue(logWindowsSizeAttrName, FileSizes.Default.WindowSize);
				Validate(ref fileSizes);

				maxNumberOfHitsInSearchResultsView = root.SafeIntValue(maxSearchResultSizeAttrName, DefaultSettingsAccessor.DefaultMaxSearchResultSize);

				multithreadedParsingDisabled = root.SafeIntValue(multithreadedParsingDisabledAttrName, DefaultSettingsAccessor.DefaultMultithreadedParsingDisabled ? 1 : 0) != 0;

				appearance.FontSize = (Appearance.LogFontSize)root.SafeIntValue(fontSizeAttrName, (int)Appearance.Default.FontSize);
				appearance.FontFamily = root.AttributeValue(fontNameAttrName, Appearance.Default.FontFamily);
				appearance.Coloring = (Appearance.ColoringMode)root.SafeIntValue(coloringAttrName, (int)Appearance.Default.Coloring);
				appearance.ColoringBrightness = (PaletteBrightness)root.SafeIntValue(coloringPaletteAttrName, (int)Appearance.Default.ColoringBrightness);
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

		void Save()
		{
			EnsureLoaded();
			using (var section = storageManager.GlobalSettingsEntry.OpenXMLSection(sectionName,
				Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen))
			{
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
			if (Changed != null)
				Changed(this, new SettingsChangeEvent(settingsPiece));
		}
	}
}
