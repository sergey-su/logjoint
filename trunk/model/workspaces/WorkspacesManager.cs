using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Workspaces
{
	public class WorkspacesManager : IWorkspacesManager
	{
		bool IWorkspacesManager.IsWorkspaceUri(Uri uri)
		{
			return false;
		}
	};
}
