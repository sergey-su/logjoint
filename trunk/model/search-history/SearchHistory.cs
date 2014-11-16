using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class SearchHistory: ISearchHistory
	{
		public SearchHistory(Persistence.IStorageEntry globalSettings)
		{
			this.globalSettings = globalSettings;

			LoadSearchHistory();
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
				value = Utils.PutInRange(0, 1000, value);
				if (value == maxItemsCount)
					return;
				maxItemsCount = value;
				ApplySizeLimit();
				SaveSearchHistory();
				FireOnChange();
			}
		}

		void ISearchHistory.Add(SearchHistoryEntry entry)
		{
			if (entry.Template.Length == 0)
				return;
			items.RemoveAll(i => i.Equals(entry));
			items.Add(entry);
			while (items.Count > maxItemsCount)
				items.RemoveAt(0);
			FireOnChange();
			SaveSearchHistory();
		}
		IEnumerable<SearchHistoryEntry> ISearchHistory.Items
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
			if (OnChanged != null)
				OnChanged(this, EventArgs.Empty);
		}

		private void LoadSearchHistory()
		{
			using (var section = globalSettings.OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				maxItemsCount = section.Data.Element(rootNodeName).SafeIntValue(maxEntriesAttrName, DefaultMaxEntries);
				items.AddRange(
					from entryNode in section.Data.Elements(rootNodeName).Elements(entryNodeName)
					let entry = new SearchHistoryEntry(entryNode)
					where entry.IsValid
					select entry
				);
				ApplySizeLimit();
			}
		}

		private void SaveSearchHistory()
		{
			using (var section = globalSettings.OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				var newContent = items.Select(e => e.Store()).ToArray();
				var root = new XElement(rootNodeName, newContent);
				root.SetAttributeValue(maxEntriesAttrName, maxItemsCount);
				section.Data.RemoveNodes();
				section.Data.Add(root);
			}
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

		private readonly Persistence.IStorageEntry globalSettings;
		private readonly List<SearchHistoryEntry> items = new List<SearchHistoryEntry>();
		private int maxItemsCount;
	}
}
