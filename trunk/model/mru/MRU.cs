using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public interface IRecentlyUsedLogs
	{
		void RegisterRecentLogEntry(ILogProvider provider);
		IEnumerable<RecentLogEntry> GetMRUList();
		Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter();
		IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories);
	};

	public class RecentlyUsedLogs : IRecentlyUsedLogs
	{
		static readonly string RecentLogsSectionName = "recent-logs";
		static readonly string RecentFactoriesSectionName = "recent-factories";


		public RecentlyUsedLogs(Persistence.IStorageEntry settingsEntry)
		{
			this.settingsEntry = settingsEntry;
			this.maxRecentLogs = 20; // todo: get rid of hardcoded numbers
			this.maxRecentFactories = 20;
		}

		public void RegisterRecentLogEntry(ILogProvider provider)
		{
			AddMRULog(provider);
			AddMRUFactory(provider);
		}

		public IEnumerable<RecentLogEntry> GetMRUList()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				foreach (var e in sect.Data.SafeElement("root").SafeElements("entry"))
				{
					RecentLogEntry entry;
					try
					{
						entry = RecentLogEntry.Parse(e.Value);
					}
					catch (RecentLogEntry.FormatNotRegistedException)
					{
						continue;
					}
					catch (InvalidConnectionParamsException)
					{
						continue;
					}
					yield return entry;
				}
			}
		}

		public Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter()
		{
			var dict = new Dictionary<ILogProviderFactory, int>();
			int mruIndex = 0;
			foreach (var f in GetRecentFactories())
				dict[f] = mruIndex++;
			return f => dict.ContainsKey(f) ? dict[f] : mruIndex;
		}

		public IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories)
		{
			var recentFactories = new List<ILogProviderFactory>(GetRecentFactories());
			List<ILogProviderFactory> requestedFactories = new List<ILogProviderFactory>(factories);
			requestedFactories.Sort((f1, f2) => LogProviderFactoryRegistry.ToString(f1).CompareTo(LogProviderFactoryRegistry.ToString(f2)));
			recentFactories.RemoveAll(f1 => !requestedFactories.Exists(f2 => f1 == f2));
			requestedFactories.RemoveAll(f1 => recentFactories.Exists(f2 => f1 == f2));
			foreach (ILogProviderFactory f in recentFactories)
				yield return f;
			foreach (ILogProviderFactory f in requestedFactories)
				yield return f;
		}

		private void AddMRUEntry(string regKey, string mruEntry, int maxEntries)
		{
			using (var sect = settingsEntry.OpenXMLSection(regKey, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				XElement root = sect.Data.Element("root");
				if (root == null)
					sect.Data.Add(root = new XElement("root"));

				List<string> mru = new List<string>();
				foreach (var e in root.Elements("entry"))
					if (!string.IsNullOrWhiteSpace(e.Value))
						mru.Add(e.Value);

				int idx = mru.IndexOf(mruEntry);
				if (idx >= 0)
				{
					for (int j = idx; j > 0; --j)
						mru[j] = mru[j - 1];
					mru[0] = mruEntry;
				}
				else
				{
					mru.Insert(0, mruEntry);
				}
				if (mru.Count > maxEntries)
					mru.RemoveAt(mru.Count - 1);
				
				root.RemoveNodes();
				foreach (string s in mru)
					root.Add(new XElement("entry", s));
			}
		}

		private void AddMRULog(ILogProvider provider)
		{
			var mruConnectionParams = provider.Factory.GetConnectionParamsToBeStoredInMRUList(provider.ConnectionParams);
			if (mruConnectionParams == null)
				return;
			AddMRUEntry(RecentLogsSectionName, new RecentLogEntry(provider.Factory, mruConnectionParams).ToString(), maxRecentLogs);
		}

		private void AddMRUFactory(ILogProvider provider)
		{
			AddMRUEntry(RecentFactoriesSectionName, RecentLogEntry.FactoryPartToString(provider.Factory), maxRecentFactories);
		}

		IEnumerable<ILogProviderFactory> GetRecentFactories()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentFactoriesSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				return 
					from e in sect.Data.SafeElement("root").SafeElements("entry")
					let f = RecentLogEntry.ParseFactoryPart(e.Value)
					where f != null
					select f;
			}
		}
		
		readonly Persistence.IStorageEntry settingsEntry;
		readonly int maxRecentLogs;
		readonly int maxRecentFactories;
	}
}
