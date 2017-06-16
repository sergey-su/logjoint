using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
	public class PresentaterPersistentState
	{
		readonly Persistence.IStorageEntry stateEntry;
		readonly string logSourceStateSectionName;


		public PresentaterPersistentState(
			Persistence.IStorageManager storageManager,
			string globalStateStorageEntryName,
			string logSourceSpecificStateSectionName
		)
		{
			this.stateEntry = storageManager.GetEntry(globalStateStorageEntryName);
			this.logSourceStateSectionName = logSourceSpecificStateSectionName;
		}

		public HashSet<string> GetVisibleTags(IEnumerable<ILogSource> sources, HashSet<string> availableTags, string[] defaultVisibleTags)
		{
			var visibleTags = LoadTags(sources);
			if (visibleTags == null)
				visibleTags = LoadTags(stateEntry, Constants.GlobalStateSectionName);
			if (visibleTags == null)
				visibleTags = new HashSet<string>(defaultVisibleTags);
			visibleTags.IntersectWith(availableTags);
			if (visibleTags.Count == 0)
				visibleTags = new HashSet<string>(availableTags);
			return visibleTags;
		}

		public void SaveVisibleTags(IEnumerable<string> visibleTags, IEnumerable<ILogSource> sources)
		{
			Action<Persistence.IStorageEntry, string> saveTags = (entry, sectionName) =>
			{
				using (var section = entry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
				{
					section.Data.Add(new XElement(
						Constants.StateRootEltName,
						new XElement(Constants.TagsEltName, visibleTags.Select(t => new XElement(Constants.TagEltName, t)))
					));
				}
			};

			foreach (var entry in sources.Select(s => s.LogSourceSpecificStorageEntry))
			{
				saveTags(entry, logSourceStateSectionName);
			}
			saveTags(stateEntry, Constants.GlobalStateSectionName);
		}

		HashSet<string> LoadTags(IEnumerable<ILogSource> sources)
		{
			HashSet<string> loadedTags = null;
			foreach (var entry in sources.Select(s => s.LogSourceSpecificStorageEntry))
			{
				var tmp = LoadTags(entry, logSourceStateSectionName);
				if (tmp != null)
					if (loadedTags == null)
						loadedTags = tmp;
					else
						loadedTags.UnionWith(tmp);
			}
			return loadedTags;
		}

		private static HashSet<string> LoadTags(Persistence.IStorageEntry entry, string sectionName)
		{
			HashSet<string> loadedTags = null;
			using (var section = entry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var tagsElt = section.Data.SafeElement(Constants.StateRootEltName).SafeElement(Constants.TagsEltName);
				if (tagsElt != null)
				{
					loadedTags = new HashSet<string>();
					foreach (var tagElt in tagsElt.SafeElements(Constants.TagEltName))
						if (!string.IsNullOrEmpty(tagElt.Value))
							loadedTags.Add(tagElt.Value);
				}
			}
			return loadedTags;
		}

		static class Constants
		{
			public const string GlobalStateSectionName = "state";
			public const string StateRootEltName = "root";
			public const string TagsEltName = "tags";
			public const string TagEltName = "tag";
		};
	};
}
