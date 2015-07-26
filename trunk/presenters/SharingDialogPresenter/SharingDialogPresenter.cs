using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.Workspaces;

namespace LogJoint.UI.Presenters.SharingDialog
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly ILogSourcesManager logSourcesManager;
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly IView view;
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessingsManager;
		DialogAvailability availability;

		public Presenter(
			ILogSourcesManager logSourcesManager,
			Workspaces.IWorkspacesManager workspacesManager,
			Preprocessing.ILogSourcesPreprocessingManager preprocessingsManager,
			IView view)
		{
			this.logSourcesManager = logSourcesManager;
			this.workspacesManager = workspacesManager;
			this.view = view;
			this.preprocessingsManager = preprocessingsManager;

			view.SetEventsHandler(this);

			logSourcesManager.OnLogSourceAdded += (sender, args) =>
			{
				UpdateAvaialibility();
			};

			logSourcesManager.OnLogSourceRemoved += (sender, args) =>
			{
				UpdateAvaialibility();
			};

			workspacesManager.CurrentWorkspaceChanged += (sender, args) =>
			{
				UpdateWorkspaceUrlEditBox();
				UpdateWorkspaceEditCotrols();
			};

			workspacesManager.StatusChanged += (sender, args) =>
			{
				UpdateWorkspaceUrlEditBox();
				UpdateDescription();
				UpdateDialogButtons();
				UpdateProgressIndicator();
				UpdateWorkspaceEditCotrols(triggeredByStatusChange: true);
			};

			UpdateAvaialibility();
			UpdateDescription();
			UpdateWorkspaceUrlEditBox();
			UpdateDialogButtons();
			UpdateWorkspaceEditCotrols();
		}


		DialogAvailability IPresenter.Availability
		{
			get { return availability; }
		}

		void IPresenter.ShowDialog()
		{
			view.Show();
		}

		public event EventHandler AvailabilityChanged;

		void IViewEvents.OnUploadButtonClicked()
		{
			var nonNetworkSource = logSourcesManager.Items.FirstOrDefault(s =>
				!s.IsDisposed && !preprocessingsManager.ConnectionRequiresDownloadPreprocessing(s.Provider.ConnectionParams));
			if (nonNetworkSource != null && !view.ShowUploadWarningDialog(string.Format(
				"Log source '{0}' does not seem to be taken from a network location. It may be unavailable for those who you want to share your workspace with.\nContinue unloading?",
				nonNetworkSource.GetShortDisplayNameWithAnnotation())))
			{
				return;
			}
			workspacesManager.SaveWorkspace(view.GetWorkspaceNameEditValue(), view.GetWorkspaceAnnotationEditValue());
		}

		static bool IsTransitionalStatus(Workspaces.WorkspacesManagerStatus status)
		{
			return status == Workspaces.WorkspacesManagerStatus.CreatingWorkspace
				|| status == Workspaces.WorkspacesManagerStatus.SavingWorkspaceData
				|| status == Workspaces.WorkspacesManagerStatus.LoadingWorkspace;
		}

		void UpdateAvaialibility()
		{
			DialogAvailability a;
			if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.Unavaliable)
				a = DialogAvailability.PermanentlyUnavaliable;
			else if (
				logSourcesManager.Items.Any() &&
				!IsTransitionalStatus(workspacesManager.Status))
				a = DialogAvailability.Available;
			else
				a = DialogAvailability.TemporarilyUnavailable;
			if (a != availability)
			{
				availability = a;
				if (AvailabilityChanged != null)
					AvailabilityChanged(this, EventArgs.Empty);
			}
		}

		void UpdateDescription()
		{
			string value = "Upload your workspace (links to logs, bookmarks, postprocessing results, etc) to the cloud so that it can be shared with others.";

			if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.AttachedToUploadedWorkspace)
			{
				value = "Your workspace was uploaded. Upload again to overwrite. Enter another name to upload a new copy.";
			}
			else if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.AttachedToDownloadedWorkspace)
			{
				value = "Your workspace (links to logs, bookmarks, postprocessing results, etc) was downloaded. Press Upload to overwrite it with your changes. Enter another name to upload a new copy.";
			}

			view.UpdateDescription(value);
		}

		void UpdateDialogButtons()
		{
			if (IsTransitionalStatus(workspacesManager.Status))
			{
				view.UpdateDialogButtons(false, "Upload", "Close");
			}
			else
			{
				view.UpdateDialogButtons(true, "Upload", "Cancel");
			}
		}


		void UpdateProgressIndicator()
		{
			string text = null;
			bool isError = false;
			string details = null;
			if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.CreatingWorkspace)
				text = "creating workspace";
			else if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.SavingWorkspaceData)
				text = "uploading data";
			else if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.LoadingWorkspace)
				text = "loading";
			else if (workspacesManager.Status == WorkspacesManagerStatus.FailedToUploadWorkspace)
			{
				text = "failed to upload";
				isError = true;
				details = workspacesManager.LastError;
			}
			else if (workspacesManager.Status == WorkspacesManagerStatus.FailedToDownloadWorkspace)
			{
				text = "failed to load workspace";
				isError = true;
				details = workspacesManager.LastError;
			}
			view.UpdateProgressIndicator(text, isError, details);
		}

		void UpdateWorkspaceUrlEditBox()
		{
			string value = "";

			string comingSoon = "(will appear soon)";
			string pressUpload = "(press Upload to obtain)";

			if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.CreatingWorkspace)
			{
				value = comingSoon;
			}
			else if (workspacesManager.Status == Workspaces.WorkspacesManagerStatus.NoWorkspace)
			{
				value = pressUpload;
			}
			else
			{
				if (workspacesManager.CurrentWorkspace != null && !string.IsNullOrEmpty(workspacesManager.CurrentWorkspace.WebUrl))
					value = workspacesManager.CurrentWorkspace.WebUrl;
				else 
				if (workspacesManager.Status == WorkspacesManagerStatus.LoadingWorkspaceData 
				 || workspacesManager.Status == WorkspacesManagerStatus.SavingWorkspaceData)
					value = comingSoon;
				else
				if (workspacesManager.Status == WorkspacesManagerStatus.FailedToUploadWorkspace
				 || workspacesManager.Status == WorkspacesManagerStatus.FailedToDownloadWorkspace)
					value = pressUpload;
			}

			bool isHint = value == comingSoon || value == pressUpload;
			view.UpdateWorkspaceUrlEditBox(value, isHint, !isHint && !string.IsNullOrEmpty(value));
		}

		void UpdateWorkspaceEditCotrols(bool triggeredByStatusChange = false)
		{
			var ws = workspacesManager.CurrentWorkspace;

			string nameEditBoxBanner = "(leave empty to generate new)";

			if (ws != null)
			{
				string nameWarning = null;
				var nameAlteration = ws.NameAlterationReason;
				if (nameAlteration == WorkspaceNameAlterationReason.Conflict)
					nameWarning = "Name was changed because proposed one confliched with already existing workspace";
				else if (nameAlteration == WorkspaceNameAlterationReason.InvalidName)
					nameWarning = "Invalid characters were removed from the name";

				view.UpdateWorkspaceEditControls(
					!IsTransitionalStatus(workspacesManager.Status),
					ws.Name,
					nameEditBoxBanner,
					nameWarning,
					ws.Annotation
				);
			}
			else
			{
				bool clearEditBoxes = triggeredByStatusChange && workspacesManager.Status == WorkspacesManagerStatus.NoWorkspace;

				view.UpdateWorkspaceEditControls(
					!IsTransitionalStatus(workspacesManager.Status),
					clearEditBoxes ? "" : view.GetWorkspaceNameEditValue(),
					nameEditBoxBanner,
					null,
					clearEditBoxes ? "" : view.GetWorkspaceAnnotationEditValue()
				);
			}
		}
	};
};