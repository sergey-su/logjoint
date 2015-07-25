using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.MRU
{
	public interface IRecentlyUsedEntities
	{
		void RegisterRecentLogEntry(ILogProvider provider);
		void RegisterRecentWorkspaceEntry(string workspaceUri, string workspaceName, string workspaceAnnotation);
		IEnumerable<IRecentlyUsedEntity> GetMRUList();
		Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter();
		IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories);
		int RecentEntriesListSizeLimit { get; set; }
		void ClearRecentLogsList();
	};

	public interface IRecentlyUsedEntity
	{
		string UserFriendlyName { get; }
		string Annotation { get; }
	};
}
