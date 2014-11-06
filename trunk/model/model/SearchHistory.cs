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

		public static readonly string SettingsKey = "search-history";

		public event EventHandler OnChanged;

		static public int MaxItemsCount
		{
			get { return 300; }
		}
		public void Add(SearchHistoryEntry entry)
		{
			if (entry.Template.Length == 0)
				return;
			items.RemoveAll(i => i.Equals(entry));
			while (items.Count >= MaxItemsCount)
				items.RemoveAt(0);
			items.Add(entry);
			FireOnChange();
			SaveSearchHistory();
		}
		public IEnumerable<SearchHistoryEntry> Items
		{
			get
			{
				for (int i = items.Count - 1; i >= 0; --i)
					yield return items[i]; 
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
				items.AddRange(
					from entryNode in section.Data.Elements(rootNodeName).Elements(entryNodeName)
					let entry = new SearchHistoryEntry(entryNode)
					where entry.IsValid
					select entry
				);
			}
		}

		private void SaveSearchHistory()
		{
			using (var section = globalSettings.OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				var newContent = section.Data.Elements(rootNodeName).Elements(entryNodeName).Select(n => new SearchHistoryEntry(n)).Union(items).Distinct().Select(e => e.Store()).ToArray();
				section.Data.RemoveNodes();
				section.Data.Add(new XElement(rootNodeName, newContent));
			}
		}

		private readonly Persistence.IStorageEntry globalSettings;
		private readonly static string rootNodeName = "search-history";
		private readonly static string entryNodeName = "entry";
		private readonly List<SearchHistoryEntry> items = new List<SearchHistoryEntry>(MaxItemsCount);
	}
}
