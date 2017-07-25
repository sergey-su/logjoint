using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;

namespace LogJoint
{
	public class UserDefinedSearchesManager : IUserDefinedSearches
	{
		readonly Dictionary<string, UserDefinedSearch> items = new Dictionary<string, UserDefinedSearch>();
		readonly Lazy<Persistence.IStorageEntry> storageEntry;
		readonly IFiltersFactory filtersFactory;

		public UserDefinedSearchesManager(
			Persistence.IStorageManager storage,
			IFiltersFactory filtersFactory
		)
		{
			this.filtersFactory = filtersFactory;
			this.storageEntry = new Lazy<Persistence.IStorageEntry>(() => storage.GetEntry("UserDefinedSearches"));

			LoadItems();
		}

		IEnumerable<IUserDefinedSearch> IUserDefinedSearches.Items
		{
			get { return items.Values; }
		}

		void LoadItems()
		{
			foreach (var sectionInfo in storageEntry.Value.EnumSections(CancellationToken.None))
			{
				using (var section = storageEntry.Value.OpenXMLSection(sectionInfo.Key, Persistence.StorageSectionOpenFlag.ReadOnly))
				{
					if (section.Data.Root == null)
						continue;
					var name = section.Data.Root.AttributeValue("name");
					if (string.IsNullOrEmpty(name))
						continue;
					var search = new UserDefinedSearch(
						name,
						filtersFactory.CreateFiltersList(section.Data.Root, FiltersListPurpose.Search)
					);
					items[name] = search;
				}
			}
		}
	};
}
