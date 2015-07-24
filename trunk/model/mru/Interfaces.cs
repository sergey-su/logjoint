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
		void RegisterRecentWorkspaceEntry(string workspaceName, string workspaceAnnotation, Uri location);
		IEnumerable<RecentLogEntry> GetMRUList();
		Func<ILogProviderFactory, int> MakeFactoryMRUIndexGetter();
		IEnumerable<ILogProviderFactory> SortFactoriesMoreRecentFirst(IEnumerable<ILogProviderFactory> factories);
		int RecentLogsListSizeLimit { get; set; }
		void ClearRecentLogsList();
	};
}
