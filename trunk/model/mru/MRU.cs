using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;

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
        const int DefaultRecentLogsListSizeLimit = 400;
        const int DefaultRecentFactoriesListSizeLimit = 20;
        readonly IChangeNotification changeNotification;
        readonly Task<Persistence.IStorageEntry> settingsEntry;
        readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
        readonly Telemetry.ITelemetryCollector telemetry;
        XDocument recentLogsDocument = new XDocument(); // immutable
        XDocument recentFactoriesDocument = new XDocument(); // immutable
        readonly Func<IReadOnlyList<IRecentlyUsedEntity>> mruList;
        readonly TaskChain tasks = new TaskChain();

        public RecentlyUsedEntities(
            Persistence.IStorageManager storageManager, ILogProviderFactoryRegistry logProviderFactoryRegistry,
            Telemetry.ITelemetryCollector telemetry, IChangeNotification changeNotification, IShutdown shutdown)
        {
            this.settingsEntry = storageManager.GlobalSettingsEntry;
            this.logProviderFactoryRegistry = logProviderFactoryRegistry;
            this.telemetry = telemetry;
            this.changeNotification = changeNotification;
            this.mruList = Selectors.Create(() => recentLogsDocument, GetMRUList);
            shutdown.Cleanup += (s, e) => shutdown.AddCleanupTask(tasks.Dispose());
            ((IRecentlyUsedEntities)this).Reload();
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

        IReadOnlyList<IRecentlyUsedEntity> IRecentlyUsedEntities.MRUList => mruList();

        IReadOnlyList<IRecentlyUsedEntity> GetMRUList(XDocument document)
        {
            var result = ImmutableArray.CreateBuilder<IRecentlyUsedEntity>();
            foreach (var e in document.SafeElement(RootNodeName).SafeElements(EntryNodeName))
            {
                if (e.AttributeValue(TypeAttrName) == WorkspaceTypeAttrValue)
                {
                    result.Add(new RecentWorkspaceEntry(
                        e.Value,
                        e.AttributeValue(NameAttrName),
                        e.AttributeValue(AnnotationAttrName),
                        e.DateTimeValue(DateAttrName)
                    ));
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
                    catch (RecentLogEntry.SerializationException ex)
                    {
                        telemetry.ReportException(ex, "broken MRU entry");
                        continue;
                    }
                    result.Add(entry);
                }
            }
            return result.ToImmutable();
        }

        int IRecentlyUsedEntities.RecentEntriesListSizeLimit
        {
            get
            {
                return recentLogsDocument.SafeElement(RootNodeName).SafeIntValue(ListSizeLimitAttrName, DefaultRecentLogsListSizeLimit);
            }
            set
            {
                value = RangeUtils.PutInRange(0, 1000, value);
                if (((IRecentlyUsedEntities)this).RecentEntriesListSizeLimit == value)
                    return;
                WriteListSizeLimit(value);
                tasks.AddTask(() => WriteOut(recentLogsDocument, RecentLogsSectionName));
            }
        }

        void IRecentlyUsedEntities.ClearRecentLogsList()
        {
            WriteEntries(CloneAndEnsureRoot(ref recentLogsDocument), new List<XElement>());
            tasks.AddTask(() => WriteOut(recentLogsDocument, RecentLogsSectionName));
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

        Task IRecentlyUsedEntities.Reload()
        {
            return tasks.AddTaskAndGetTail(async () =>
            {
                var storageEntry = await settingsEntry;
                await using (var sect = await storageEntry.OpenXMLSection(RecentLogsSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
                {
                    recentLogsDocument = sect.Data;
                }
                await using (var sect = await storageEntry.OpenXMLSection(RecentFactoriesSectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
                {
                    recentFactoriesDocument = sect.Data;
                }
                changeNotification.Post();
            });
        }

        async Task WriteOut(XDocument document, string sectionName)
        {
            await using var sect = await (await settingsEntry).OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadWrite);
            sect.Data.ReplaceNodes(document.Nodes());
        }

        private void AddOrReplaceEntry(ref XDocument document, string sectionName,
            XElement mruEntry, Func<XElement, XElement, bool> comparer, int defaultSizeLimit, bool updateExisting)
        {
            XElement root = CloneAndEnsureRoot(ref document);
            int maxEntries = root.IntValue(ListSizeLimitAttrName, defaultSizeLimit);
            var mru = ReadEntries(root);
            if (updateExisting)
                Replace(mru, mruEntry, comparer);
            else
                InsertOrMakeFirst(mru, mruEntry, comparer);
            ApplySizeLimit(mru, maxEntries);
            WriteEntries(root, mru);
            XDocument documentToWriteOut = document;
            tasks.AddTask(() => WriteOut(documentToWriteOut, sectionName));
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

        static XElement CloneAndEnsureRoot(ref XDocument document)
        {
            document = new XDocument(document);
            XElement root = document.Element(RootNodeName);
            if (root == null)
                document.Add(root = new XElement(RootNodeName));
            return root;
        }

        void ApplySizeLimit(List<XElement> mru, int limit)
        {
            if (mru.Count > limit)
                mru.RemoveRange(limit, mru.Count - limit);
        }

        private void WriteListSizeLimit(int newLimit)
        {
            var root = CloneAndEnsureRoot(ref recentLogsDocument);
            root.SetAttributeValue(ListSizeLimitAttrName, newLimit);
            var mru = ReadEntries(root);
            ApplySizeLimit(mru, newLimit);
            WriteEntries(root, mru);
        }

        private void AddOrReplaceLog(ILogProvider provider, string annotation, bool updateExisting)
        {
            var mruConnectionParams = provider.Factory.GetConnectionParamsToBeStoredInMRUList(provider.ConnectionParams);
            if (mruConnectionParams == null)
                return;
            AddOrReplaceEntry(
                ref recentLogsDocument,
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
                ref recentLogsDocument,
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
                ref recentFactoriesDocument,
                RecentFactoriesSectionName,
                new XElement(EntryNodeName, RecentLogEntry.FactoryPartToString(provider.Factory)),
                (e1, e2) => e1.SafeValue() == e2.SafeValue(),
                DefaultRecentFactoriesListSizeLimit,
                updateExisting: false
            );
        }

        IEnumerable<ILogProviderFactory> GetRecentFactories()
        {
            return
                from e in recentFactoriesDocument.SafeElement(RootNodeName).SafeElements(EntryNodeName)
                let f = RecentLogEntry.ParseFactoryPart(logProviderFactoryRegistry, e.Value)
                where f != null
                select f;
        }
    }
}
