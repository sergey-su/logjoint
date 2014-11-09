using System.Xml.Linq;

namespace LogJoint.Settings
{
	public class GlobalSettingsAccessor : IGlobalSettingsAccessor
	{
		Persistence.IStorageEntry persistenceEntry;
		const string sectionName = "settings";
		const string rootNodeName = "root";
		const string fullLoadingSizeThresholdAttrName = "full-loading-size-threshold";
		const string logWindowsSizeAttrName = "log-window-size";
		const string maxSearchResultSizeAttrName = "max-search-result-size";
		const string multithreadedParsingDisabledAttrName = "multithreaded-parsing-disabled";

		const int MaxMaxSearchResultSize = DefaultSettingsAccessor.DefaultMaxSearchResultSize * 100;

		bool loaded;
		FileSizes fileSizes;
		int maxNumberOfHitsInSearchResultsView;
		bool multithreadedParsingDisabled;

		public GlobalSettingsAccessor(Persistence.IStorageEntry persistenceEntry)
		{
			this.persistenceEntry = persistenceEntry;
		}

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
				value = Utils.PutInRange(1, MaxMaxSearchResultSize, value);
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

		void EnsureLoaded()
		{
			if (loaded)
				return;
			using (var section = persistenceEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var root = section.Data.Element(rootNodeName);

				fileSizes.Threshold = root.SafeIntValue(fullLoadingSizeThresholdAttrName, FileSizes.Default.Threshold);
				fileSizes.WindowSize = root.SafeIntValue(logWindowsSizeAttrName, FileSizes.Default.WindowSize);
				Validate(ref fileSizes);

				maxNumberOfHitsInSearchResultsView = root.SafeIntValue(maxSearchResultSizeAttrName, DefaultSettingsAccessor.DefaultMaxSearchResultSize);

				multithreadedParsingDisabled = root.SafeIntValue(multithreadedParsingDisabledAttrName, DefaultSettingsAccessor.DefaultMultithreadedParsingDisabled ? 1 : 0) != 0;
			}
			loaded = true;
		}

		void Save()
		{
			EnsureLoaded();
			using (var section = persistenceEntry.OpenXMLSection(sectionName,
				Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen))
			{
				section.Data.Add(new XElement(
					rootNodeName,
					new XAttribute(fullLoadingSizeThresholdAttrName, fileSizes.Threshold),
					new XAttribute(logWindowsSizeAttrName, fileSizes.WindowSize),
					new XAttribute(maxSearchResultSizeAttrName, maxNumberOfHitsInSearchResultsView),
					new XAttribute(multithreadedParsingDisabledAttrName, multithreadedParsingDisabled ? "1" : "0")
				));
			}
		}

		static void Validate(ref FileSizes fileSizes)
		{
			fileSizes.WindowSize = Utils.PutInRange(FileSizes.MinWindowSize, FileSizes.MaxWindowSize, fileSizes.WindowSize);
			fileSizes.Threshold = Utils.PutInRange(fileSizes.WindowSize, FileSizes.MaxThreshold, fileSizes.Threshold);
		}

		static bool Differ(FileSizes val1, FileSizes val2)
		{
			return val1.Threshold != val2.Threshold || val1.WindowSize != val2.WindowSize;
		}
	}
}
