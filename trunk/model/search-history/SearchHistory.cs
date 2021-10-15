using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public class SearchHistory: ISearchHistory
	{
		public SearchHistory(
			Task<Persistence.IStorageEntry> globalSettings, 
			IUserDefinedSearches userDefinedSearches,
			IShutdown shutdown
		)
		{
			this.globalSettings = globalSettings;
			this.userDefinedSearches = userDefinedSearches;
			shutdown.Cleanup += (s, e) => shutdown.AddCleanupTask(tasks.Dispose());

			tasks.AddTask(LoadSearchHistory);
		}

		public event EventHandler OnChanged;

		int ISearchHistory.MaxCount
		{
			get
			{
				return maxItemsCount;
			}
			set
			{
				value = RangeUtils.PutInRange(0, 1000, value);
				if (value == maxItemsCount)
					return;
				maxItemsCount = value;
				ApplySizeLimit();
				SaveSearchHistory();
				FireOnChange();
			}
		}

		void ISearchHistory.Add(ISearchHistoryEntry entry)
		{
			if (!entry.IsValid)
				return;
			items.RemoveAll(i => i.Equals(entry));
			items.Add(entry);
			while (items.Count > maxItemsCount)
				items.RemoveAt(0);
			FireOnChange();
			SaveSearchHistory();
		}
		IEnumerable<ISearchHistoryEntry> ISearchHistory.Items
		{
			get
			{
				for (int i = items.Count - 1; i >= 0; --i)
					yield return items[i]; 
			}
		}

		int ISearchHistory.Count
		{
			get { return items.Count; }
		}

		void ISearchHistory.Clear()
		{
			if (items.Count > 0)
			{
				items.Clear();
				SaveSearchHistory();
				FireOnChange();
			}
		}

		void FireOnChange()
		{
			OnChanged?.Invoke(this, EventArgs.Empty);
		}

		private async Task LoadSearchHistory()
		{
			using (var section = await (await globalSettings).OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				maxItemsCount = section.Data.Element(rootNodeName).SafeIntValue(maxEntriesAttrName, DefaultMaxEntries);
				items.AddRange(
					from entryNode in section.Data.Elements(rootNodeName).Elements(entryNodeName)
					let entry =
						(ISearchHistoryEntry)UserDefinedSearchHistoryEntry.TryLoad(entryNode, userDefinedSearches) ??
						new SearchHistoryEntry(entryNode)
					where entry.IsValid
					select entry
				);
				ApplySizeLimit();
			}
		}

		private void SaveSearchHistory()
		{
			tasks.AddTask(async () =>
			{
				using (var section = await (await globalSettings).OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadWrite))
				{
					var newContent = items.Select(e =>
					{
						var xml = new XElement(entryNodeName);
						e.Save(xml);
						return xml;
					}).ToArray();
					var root = new XElement(rootNodeName, newContent);
					root.SetAttributeValue(maxEntriesAttrName, maxItemsCount);
					section.Data.RemoveNodes();
					section.Data.Add(root);
				}
			});
		}

		bool ApplySizeLimit()
		{
			if (items.Count <= maxItemsCount)
				return false;
			items.RemoveRange(maxItemsCount, items.Count - maxItemsCount);
			return true;
		}

		private readonly static string SettingsKey = "search-history";
		private readonly static string rootNodeName = "search-history";
		private readonly static string entryNodeName = "entry";
		private readonly static string maxEntriesAttrName = "max-entries";
		private const int DefaultMaxEntries = 200;

		private readonly Task<Persistence.IStorageEntry> globalSettings;
		private readonly IUserDefinedSearches userDefinedSearches;
		private readonly List<ISearchHistoryEntry> items = new List<ISearchHistoryEntry>();
		private int maxItemsCount;
		private readonly TaskChain tasks = new TaskChain();
	}
}
