using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class UserDefinedSearchesManager : IUserDefinedSearches, IUserDefinedSearchesInternal
	{
		readonly Dictionary<string, IUserDefinedSearch> items = 
			new Dictionary<string, IUserDefinedSearch>(StringComparer.CurrentCultureIgnoreCase);
		readonly Lazy<Persistence.IStorageEntry> storageEntry;
		readonly IFiltersFactory filtersFactory;
		readonly AsyncInvokeHelper saveInvoker;
		const string sectionName = "items";

		public UserDefinedSearchesManager(
			Persistence.IStorageManager storage,
			IFiltersFactory filtersFactory,
			IInvokeSynchronization modelThreadSynchronization
		)
		{
			this.filtersFactory = filtersFactory;
			this.storageEntry = new Lazy<Persistence.IStorageEntry>(() => storage.GetEntry("UserDefinedSearches"));
			this.saveInvoker = new AsyncInvokeHelper(modelThreadSynchronization, (Action)SaveItems)
			{
				ForceAsyncInvocation = true
			};

			LoadItems();
		}

		IEnumerable<IUserDefinedSearch> IUserDefinedSearches.Items
		{
			get { return items.Values; }
		}

		bool IUserDefinedSearches.ContainsItem(string name)
		{
			return items.ContainsKey(name);
		}

		void IUserDefinedSearchesInternal.OnNameChanged(IUserDefinedSearch sender, string oldName)
		{
			items.Remove(oldName);
			items.Add(sender.Name, sender);
			saveInvoker.Invoke();
		}

		void IUserDefinedSearchesInternal.OnFiltersChanged(IUserDefinedSearch sender)
		{
			saveInvoker.Invoke();
		}

		void LoadItems()
		{
			items.Clear();
			using (var section = storageEntry.Value.OpenXMLSection(
				sectionName, 
				Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				if (section.Data.Root == null)
					return;
				foreach (var itemElt in section.Data.Root.Elements("item"))
				{
					var name = itemElt.AttributeValue("name");
					if (string.IsNullOrEmpty(name))
						continue;
					if (items.ContainsKey(name))
						continue;
					var search = new UserDefinedSearch(
						this,
						name,
						filtersFactory.CreateFiltersList(itemElt, FiltersListPurpose.Search)
					);
					items[name] = search;
				}
			}
		}

		void SaveItems()
		{
			using (var section = storageEntry.Value.OpenXMLSection(
				sectionName,
				Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
			{
				section.Data.Add(
					new XElement(
						"root",
						items.Values.Select(item => 
						{
							var itemElt = new XElement("item", new XAttribute("name", item.Name));
							item.Filters.Save(itemElt);
							return itemElt;
						})
					)
				);
			}
		}
	};
}
