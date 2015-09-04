using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LogJoint.Workspaces.Backend
{
	class NoWorkspacesBackend: IBackendAccess
	{
		bool IBackendAccess.IsConfigured
		{
			get { return false; }
		}

		bool IBackendAccess.IsValidWorkspaceUri(Uri uri)
		{
			return false;
		}

		Task<CreatedWorkspaceDTO> IBackendAccess.CreateWorkspace(WorkspaceDTO dto)
		{
			throw new NotImplementedException();
		}

		Task<WorkspaceDTO> IBackendAccess.GetWorkspace(string workspaceUri, CancellationToken cancellation)
		{
			throw new NotImplementedException();
		}

		Task IBackendAccess.UploadEntriesArchive(string destinationRef, Stream source)
		{
			throw new NotImplementedException();
		}
			
		Task IBackendAccess.GetEntriesArchive(string uri, Stream destinationStream, CancellationToken cancellation)
		{
			throw new NotImplementedException();
		}
	}
}
