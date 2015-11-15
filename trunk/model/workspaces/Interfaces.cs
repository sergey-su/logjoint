using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;

namespace LogJoint.Workspaces
{
	public interface IWorkspacesManager
	{
		bool IsWorkspaceUri(Uri uri);
		WorkspacesManagerStatus Status { get; }
		WorkspaceInfo CurrentWorkspace { get; }
		Task SaveWorkspace(string name, string annotation);
		Task<WorkspaceEntryInfo[]> LoadWorkspace(string workspaceUri, CancellationToken cancellation);
		string LastError { get; }
		void DetachFromWorkspace();

		event EventHandler StatusChanged;
		event EventHandler CurrentWorkspaceChanged;
	};

	public class WorkspaceInfo
	{
		public string Name;
		public string Annotation;
		public string Uri;
		public string WebUrl;
		public WorkspaceNameAlterationReason NameAlterationReason;
	};

	public enum WorkspacesManagerStatus
	{
		Unavailable,
		NoWorkspace,
		CreatingWorkspace,
		SavingWorkspaceData,
		LoadingWorkspace,
		LoadingWorkspaceData,
		AttachedToUploadedWorkspace,
		FailedToUploadWorkspace,
		AttachedToDownloadedWorkspace,
		FailedToDownloadWorkspace
	}

	public enum WorkspaceNameAlterationReason
	{
		None,
		Conflict,
		InvalidName
	};

	public class WorkspaceEntryInfo
	{
		public RecentLogEntry Log;
		public bool IsHiddenLog;
	};

	namespace Backend
	{
		public interface IBackendAccess
		{
			bool IsConfigured { get; }
			bool IsValidWorkspaceUri(Uri uri);
			Task<CreatedWorkspaceDTO> CreateWorkspace(WorkspaceDTO dto);
			Task UploadEntriesArchive(string destinationUri, Stream source);
			Task<WorkspaceDTO> GetWorkspace(string workspaceUri, CancellationToken cancellation);
			Task GetEntriesArchive(string uri, Stream destinationStream, CancellationToken cancellation);
		}
	}
}
