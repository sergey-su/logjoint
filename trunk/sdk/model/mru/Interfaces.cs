using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace LogJoint.MRU
{
	public interface IRecentlyUsedEntities
	{
		void RegisterRecentLogEntry(ILogProvider provider, string annotation);
		void UpdateRecentLogEntry(ILogProvider provider, string annotation);
		void RegisterRecentWorkspaceEntry(string workspaceUri, string workspaceName, string workspaceAnnotation);
		IReadOnlyList<IRecentlyUsedEntity> MRUList { get; }
		Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter();
		IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories);
		int RecentEntriesListSizeLimit { get; set; }
		void ClearRecentLogsList();
		Task Reload();
	};

	public interface IRecentlyUsedEntity
	{
		string UserFriendlyName { get; }
		string Annotation { get; }
		RecentlyUsedEntityType Type { get; }
		DateTime? UseTimestampUtc { get; }
		ILogProviderFactory Factory { get; }
		IConnectionParams ConnectionParams { get; }
	};

	public enum RecentlyUsedEntityType
	{
		Log,
		Workspace
	};
}
