using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.MRU
{
	public class RecentlyUsedEntities : IRecentlyUsedEntities
	{
		static readonly string RecentLogsSectionName = "recent-logs";
		static readonly string RecentFactoriesSectionName = "recent-factories";
		static readonly string RootNodeName = "root";
		static readonly string EntryNodeName = "entry";
		static readonly string ListSizeLimitAttrName = "max-nr-of-entries";
		static readonly string TypeAttrName = "type";
		static readonly string WorkspaceTypeAttrValue = "ws";
		static readonly string LogTypeAttrValue = "log";
		static readonly string AnnotationAttrName = "annotation";
		static readonly string DateAttrName = "date";
		static readonly string NameAttrName = "name";
		const int DefaultRecentLogsListSizeLimit = 100;
		const int DefaultRecentFactoriesListSizeLimit = 20;

		public RecentlyUsedEntities(Persistence.IStorageManager storageManager, ILogProviderFactoryRegistry logProviderFactoryRegistry)
		{
			this.settingsEntry = storageManager.GlobalSettingsEntry;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
		}

		void IRecentlyUsedEntities.RegisterRecentLogEntry(ILogProvider provider, string annotation)
		{
			AddOrReplaceLog(provider, annotation, updateExisting: false);
			AddFactory(provider);
		}

		void IRecentlyUsedEntities.UpdateRecentLogEntry(ILogProvider provider, string annotation)
		{
			AddOrReplaceLog(provider, annotation, updateExisting: true);
		}

		void IRecentlyUsedEntities.RegisterRecentWorkspaceEntry(string workspaceUrl, string workspaceName, string workspaceAnnotation)
		{
			AddOrReplaceWorkspace(workspaceUrl, workspaceName, workspaceAnnotation);
		}

		int IRecentlyUsedEntities.GetMRUListSize()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				return sect.Data.SafeElement(RootNodeName).SafeElements(EntryNodeName).Count();
			}
		}

		IEnumerable<IRecentlyUsedEntity> IRecentlyUsedEntities.GetMRUList()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				foreach (var e in sect.Data.SafeElement(RootNodeName).SafeElements(EntryNodeName))
				{
					if (e.AttributeValue(TypeAttrName) == WorkspaceTypeAttrValue)
					{
						yield return new RecentWorkspaceEntry(
							e.Value,
							e.AttributeValue(NameAttrName),
							e.AttributeValue(AnnotationAttrName),
							e.DateTimeValue(DateAttrName)
						);
					}
					else
					{
						RecentLogEntry entry;
						try
						{
							entry = new RecentLogEntry(logProviderFactoryRegistry, 
								e.Value, e.AttributeValue(AnnotationAttrName), e.DateTimeValue(DateAttrName));
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
		}

		int IRecentlyUsedEntities.RecentEntriesListSizeLimit
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
				value = RangeUtils.PutInRange(0, 1000, value);
				if (maxRecentLogs.HasValue && maxRecentLogs.Value == value)
					return;
				maxRecentLogs = value;
				WriteListSizeLimit(RecentLogsSectionName, value);
			}
		}

		void IRecentlyUsedEntities.ClearRecentLogsList()
		{
			ClearMRUEntries(RecentLogsSectionName);
		}

		Func<ILogProviderFactory, int> IRecentlyUsedEntities.MakeFactoryMRUIndexGetter()
		{
			var dict = new Dictionary<ILogProviderFactory, int>();
			int mruIndex = 0;
			foreach (var f in GetRecentFactories())
				dict[f] = mruIndex++;
			return f => dict.ContainsKey(f) ? dict[f] : mruIndex;
		}

		IEnumerable<ILogProviderFactory> IRecentlyUsedEntities.SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories)
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
				WriteEntries(EnsureRoot(sect), new List<XElement>());
			}
		}

		private void AddOrReplaceEntry(string sectionName, XElement mruEntry, Func<XElement, XElement, bool> comparer, int defaultSizeLimit, bool updateExisting)
		{
			using (var sect = settingsEntry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				XElement root = EnsureRoot(sect);
				int maxEntries = root.IntValue(ListSizeLimitAttrName, defaultSizeLimit);
				var mru = ReadEntries(root);
				if (updateExisting)
					Replace(mru, mruEntry, comparer);
				else
					InsertOrMakeFirst(mru, mruEntry, comparer);
				ApplySizeLimit(mru, maxEntries);
				WriteEntries(root, mru);
			}
		}

		private static void InsertOrMakeFirst(List<XElement> mru, XElement mruEntry, Func<XElement, XElement, bool> comparer)
		{
			int idx = mru.IndexOf(e => comparer(e, mruEntry)).GetValueOrDefault(-1);
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

		private static bool Replace(List<XElement> mru, XElement mruEntry, Func<XElement, XElement, bool> comparer)
		{
			int idx = mru.IndexOf(e => comparer(e, mruEntry)).GetValueOrDefault(-1);
			if (idx < 0)
				return false;
			mru[idx] = mruEntry;
			return true;
		}

		List<XElement> ReadEntries(XElement root)
		{
			var mru = new List<XElement>();
			foreach (var e in root.Elements(EntryNodeName))
				if (!string.IsNullOrWhiteSpace(e.Value))
					mru.Add(e);
			return mru;
		}

		void WriteEntries(XElement root, List<XElement> mru)
		{
			root.RemoveNodes();
			foreach (var s in mru)
				root.Add(s);
		}

		XElement EnsureRoot(Persistence.IXMLStorageSection sect)
		{
			XElement root = sect.Data.Element(RootNodeName);
			if (root == null)
				sect.Data.Add(root = new XElement(RootNodeName));
			return root;
		}

		void ApplySizeLimit(List<XElement> mru, int limit)
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
				var mru = ReadEntries(root);
				ApplySizeLimit(mru, newLimit);
				WriteEntries(root, mru);
			}
		}

		private void AddOrReplaceLog(ILogProvider provider, string annotation, bool updateExisting)
		{
			var mruConnectionParams = provider.Factory.GetConnectionParamsToBeStoredInMRUList(provider.ConnectionParams);
			if (mruConnectionParams == null)
				return;
			AddOrReplaceEntry(
				RecentLogsSectionName,
				new XElement(
					EntryNodeName,
					new XAttribute(TypeAttrName, LogTypeAttrValue),
					new XAttribute(AnnotationAttrName, annotation ?? ""),
					DateTime.UtcNow.ToDateTimeAttribute(DateAttrName),
					new RecentLogEntry(provider.Factory, mruConnectionParams, annotation, null).ToString()
				),
				(e1, e2) => e1.SafeValue() == e2.SafeValue(),
				DefaultRecentLogsListSizeLimit,
				updateExisting
			);
		}

		private void AddOrReplaceWorkspace(string workspaceUrl, string workspaceName, string workspaceAnnotation)
		{
			AddOrReplaceEntry(
				RecentLogsSectionName,
				new XElement(
					EntryNodeName,
					new XAttribute(TypeAttrName, WorkspaceTypeAttrValue),
					new XAttribute(NameAttrName, workspaceName),
					new XAttribute(AnnotationAttrName, workspaceAnnotation),
					DateTime.UtcNow.ToDateTimeAttribute(DateAttrName),
					workspaceUrl
				),
				(e1, e2) => e1.SafeValue() == e2.SafeValue(),
				DefaultRecentLogsListSizeLimit,
				updateExisting: false
			);
		}

		private void AddFactory(ILogProvider provider)
		{
			AddOrReplaceEntry(
				RecentFactoriesSectionName, 
				new XElement(EntryNodeName, RecentLogEntry.FactoryPartToString(provider.Factory)),
				(e1, e2) => e1.SafeValue() == e2.SafeValue(),
				DefaultRecentFactoriesListSizeLimit,
				updateExisting: false
			);
		}

		IEnumerable<ILogProviderFactory> GetRecentFactories()
		{
			using (var sect = settingsEntry.OpenXMLSection(RecentFactoriesSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				return 
					from e in sect.Data.SafeElement(RootNodeName).SafeElements(EntryNodeName)
					let f = RecentLogEntry.ParseFactoryPart(logProviderFactoryRegistry, e.Value)
					where f != null
					select f;
			}
		}
		
		readonly Persistence.IStorageEntry settingsEntry;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		int? maxRecentLogs;
	}
}
