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
		int RecentLogsListSizeLimit { get; set; }
		void ClearRecentLogsList();
	};

	public class RecentlyUsedLogs : IRecentlyUsedLogs
	{
		static readonly string RecentLogsSectionName = "recent-logs";
		static readonly string RecentFactoriesSectionName = "recent-factories";
		static readonly string RootNodeName = "root";
		static readonly string EntryNodeName = "entry";
		static readonly string ListSizeLimitAttrName = "max-entries";
		const int DefaultRecentLogsListSizeLimit = 20;
		const int DefaultRecentFactoriesListSizeLimit = 20;

		public RecentlyUsedLogs(Persistence.IStorageEntry settingsEntry)
		{
			this.settingsEntry = settingsEntry;
		}

		void IRecentlyUsedLogs.RegisterRecentLogEntry(ILogProvider provider)
		{
			AddMRULog(provider);
			AddMRUFactory(provider);
		}

		IEnumerable<RecentLogEntry> IRecentlyUsedLogs.GetMRUList()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				foreach (var e in sect.Data.SafeElement(RootNodeName).SafeElements(EntryNodeName))
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

		int IRecentlyUsedLogs.RecentLogsListSizeLimit
		{
			get
			{
				if (maxRecentLogs == null)
				{
					using (var sect = settingsEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
					{
						maxRecentLogs = sect.Data.SafeElement(RootNodeName).SafeIntValue(ListSizeLimitAttrName, DefaultRecentLogsListSizeLimit);
					}
				}
				return maxRecentLogs.Value;
			}
			set
			{
				value = Utils.PutInRange(0, 1000, value);
				if (maxRecentLogs.HasValue && maxRecentLogs.Value == value)
					return;
				maxRecentLogs = value;
				WriteListSizeLimit(RecentLogsSectionName, value);
			}
		}

		void IRecentlyUsedLogs.ClearRecentLogsList()
		{
			ClearMRUEntries(RecentLogsSectionName);
		}

		Func<ILogProviderFactory, int> IRecentlyUsedLogs.MakeFactoryMRUIndexGetter()
		{
			var dict = new Dictionary<ILogProviderFactory, int>();
			int mruIndex = 0;
			foreach (var f in GetRecentFactories())
				dict[f] = mruIndex++;
			return f => dict.ContainsKey(f) ? dict[f] : mruIndex;
		}

		IEnumerable<ILogProviderFactory> IRecentlyUsedLogs.SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories)
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

		void ClearMRUEntries(string sectionName)
		{
			using (var sect = settingsEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				WriteEntries(EnsureRoot(sect), new List<string>());
			}
		}

		private void AddMRUEntry(string sectionName, string mruEntry, int defaultSizeLimit)
		{
			using (var sect = settingsEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				XElement root = EnsureRoot(sect);
				int maxEntries = root.IntValue(ListSizeLimitAttrName, defaultSizeLimit);
				List<string> mru = ReadEntries(root);
				InsertOrMakeFirst(mru, mruEntry);
				ApplySizeLimit(mru, maxEntries);
				WriteEntries(root, mru);
			}
		}

		private static void InsertOrMakeFirst(List<string> mru, string mruEntry)
		{
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
		}

		List<string> ReadEntries(XElement root)
		{
			List<string> mru = new List<string>();
			foreach (var e in root.Elements(EntryNodeName))
				if (!string.IsNullOrWhiteSpace(e.Value))
					mru.Add(e.Value);
			return mru;
		}

		void WriteEntries(XElement root, List<string> mru)
		{
			root.RemoveNodes();
			foreach (string s in mru)
				root.Add(new XElement(EntryNodeName, s));
		}

		XElement EnsureRoot(Persistence.IXMLStorageSection sect)
		{
			XElement root = sect.Data.Element(RootNodeName);
			if (root == null)
				sect.Data.Add(root = new XElement(RootNodeName));
			return root;
		}

		void ApplySizeLimit(List<string> mru, int limit)
		{
			if (mru.Count > limit)
				mru.RemoveRange(limit, mru.Count - limit);
		}

		private void WriteListSizeLimit(string sectionName, int newLimit)
		{
			using (var sect = settingsEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				var root = EnsureRoot(sect);
				root.SetAttributeValue(ListSizeLimitAttrName, newLimit);
				List<string> mru = ReadEntries(root);
				ApplySizeLimit(mru, newLimit);
				WriteEntries(root, mru);
			}
		}

		private void AddMRULog(ILogProvider provider)
		{
			var mruConnectionParams = provider.Factory.GetConnectionParamsToBeStoredInMRUList(provider.ConnectionParams);
			if (mruConnectionParams == null)
				return;
			AddMRUEntry(RecentLogsSectionName, new RecentLogEntry(provider.Factory, mruConnectionParams).ToString(), DefaultRecentLogsListSizeLimit);
		}

		private void AddMRUFactory(ILogProvider provider)
		{
			AddMRUEntry(RecentFactoriesSectionName, RecentLogEntry.FactoryPartToString(provider.Factory), DefaultRecentFactoriesListSizeLimit);
		}

		IEnumerable<ILogProviderFactory> GetRecentFactories()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentFactoriesSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				return 
					from e in sect.Data.SafeElement(RootNodeName).SafeElements(EntryNodeName)
					let f = RecentLogEntry.ParseFactoryPart(e.Value)
					where f != null
					select f;
			}
		}
		
		readonly Persistence.IStorageEntry settingsEntry;
		int? maxRecentLogs;
	}
}
