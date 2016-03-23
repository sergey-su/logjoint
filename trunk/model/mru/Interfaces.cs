using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.MRU
{
	public interface IRecentlyUsedEntities
	{
		void RegisterRecentLogEntry(ILogProvider provider, string annotation);
		void UpdateRecentLogEntry(ILogProvider provider, string annotation);
		void RegisterRecentWorkspaceEntry(string workspaceUri, string workspaceName, string workspaceAnnotation);
		IEnumerable<IRecentlyUsedEntity> GetMRUList();
		int GetMRUListSize();
		Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter();
		IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories);
		int RecentEntriesListSizeLimit { get; set; }
		void ClearRecentLogsList();
	};

	public interface IRecentlyUsedEntity
	{
		string UserFriendlyName { get; }
		string Annotation { get; }
		RecentlyUsedEntityType Type { get; }
		DateTime? UseTimestampUtc { get; }
		IConnectionParams ConnectionParams { get; }
	};

	public enum RecentlyUsedEntityType
	{
		Log,
		Workspace
	};
}
