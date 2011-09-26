using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Win32;

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
		static readonly string SettingsRegKey = @"Software\LogJoint";
		static readonly string MRULogsRegKey = SettingsRegKey + @"\MRU";
		static readonly string MRUFactoriesRegKey = SettingsRegKey + @"\MRUFactories";


		public void RegisterRecentLogEntry(ILogProvider provider)
		{
			AddMRULog(provider);
			AddMRUFactory(provider);
		}

		public IEnumerable<RecentLogEntry> GetMRUList()
		{
			return GetMRUList(MRULogsRegKey);
		}

		public Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter()
		{
			var dict = new Dictionary<ILogProviderFactory, int>();
			int mruIndex = 0;
			foreach (var e in GetMRUList(MRUFactoriesRegKey))
				dict[e.Factory] = mruIndex++;
			return f => dict.ContainsKey(f) ? dict[f] : mruIndex;
		}

		public IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories)
		{
			var recentFactories = new List<ILogProviderFactory>(GetMRUList(MRUFactoriesRegKey).Select(e => e.Factory));
			List<ILogProviderFactory> requestedFactories = new List<ILogProviderFactory>(factories);
			requestedFactories.Sort((f1, f2) => LogProviderFactoryRegistry.ToString(f1).CompareTo(LogProviderFactoryRegistry.ToString(f2)));
			recentFactories.RemoveAll(f1 => !requestedFactories.Exists(f2 => f1 == f2));
			requestedFactories.RemoveAll(f1 => recentFactories.Exists(f2 => f1 == f2));
			foreach (ILogProviderFactory f in recentFactories)
				yield return f;
			foreach (ILogProviderFactory f in requestedFactories)
				yield return f;
		}

		private static void AddMRUEntry(string regKey, string mruEntry)
		{
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regKey))
			{
				if (key != null)
				{
					List<string> mru = new List<string>();
					foreach (string s in key.GetValueNames())
					{
						string tmp = key.GetValue(s, "") as string;
						if (!string.IsNullOrEmpty(tmp))
							mru.Add(tmp);
						key.DeleteValue(s);
					}
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
					if (mru.Count > 20)
						mru.RemoveAt(mru.Count - 1);
					int i = 0;
					foreach (string s in mru)
						key.SetValue((i++).ToString("00"), s);
				}
			}
		}

		private static void AddMRULog(ILogProvider provider)
		{
			var mruConnectionParams = provider.Factory.GetConnectionParamsToBeStoredInMRUList(provider.Stats.ConnectionParams);
			if (mruConnectionParams == null)
				return;
			AddMRUEntry(MRULogsRegKey, new RecentLogEntry(provider.Factory, mruConnectionParams).ToString());
		}

		private static void AddMRUFactory(ILogProvider provider)
		{
			AddMRUEntry(MRUFactoriesRegKey, new RecentLogEntry(provider.Factory, new ConnectionParams()).ToString());
		}
		
		private static IEnumerable<RecentLogEntry> GetMRUList(string regKey)
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regKey, false))
				if (key != null)
				{
					foreach (string s in key.GetValueNames())
					{
						string fname = key.GetValue(s, "") as string;
						if (string.IsNullOrEmpty(fname))
							continue;
						RecentLogEntry entry;
						try
						{
							entry = new RecentLogEntry(fname);
						}
						catch (RecentLogEntry.FormatNotRegistedException)
						{
							continue;
						}
						yield return entry;
					}
				}
		}
	}
}
