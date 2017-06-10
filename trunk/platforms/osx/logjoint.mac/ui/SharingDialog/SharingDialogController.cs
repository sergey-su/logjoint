
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.SharingDialog;

namespace LogJoint.UI
{
	public partial class SharingDialogController : AppKit.NSWindowController, IView
	{
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public SharingDialogController(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SharingDialogController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public SharingDialogController()
			: base("SharingDialog")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed window accessor
		public new SharingDialog Window
		{
			get
			{
				return (SharingDialog)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			Window.WillClose += (object sender, EventArgs e) => 
			{
				NSApplication.SharedApplication.StopModal();
			};
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			Window.GetHashCode();
		}

		void IView.Show()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.UpdateDescription(string value)
		{
			descriptionLabel.StringValue = value;
		}

		void IView.UpdateWorkspaceUrlEditBox(string value, bool isHintValue, bool allowCopying)
		{
			urlEditbox.StringValue = value;
			copyUrlButton.Enabled = allowCopying;
			urlEditbox.Enabled = !isHintValue;
		}

		void IView.UpdateDialogButtons(bool uploadEnabled, string uploadText, string cancelText)
		{
			uploadButton.Enabled = uploadEnabled;
			uploadButton.Title = uploadText;
			cancelButton.Title = cancelText;
		}

		void IView.UpdateProgressIndicator(string text, bool isError, string details)
		{
			bool progressControlsHidden = text == null;
			statusLabel.Hidden = progressControlsHidden;
			progressIndicator.Hidden = progressControlsHidden;
			warningSign.Hidden = progressControlsHidden;
			statusDetailsLabel.Hidden = progressControlsHidden;
			if (progressControlsHidden)
				return;
			statusLabel.StringValue = text ?? "";
			progressIndicator.Hidden = isError;
			warningSign.Hidden = !isError;
			statusDetailsLabel.Hidden = string.IsNullOrEmpty(details);
			statusDetailsLabel.StringValue = "details";
			statusDetailsLabel.LinkClicked = (s, e) => eventsHandler.OnStatusLinkClicked();
		}

		string IView.GetWorkspaceNameEditValue()
		{
			return idEditbox.StringValue;
		}

		string IView.GetWorkspaceAnnotationEditValue()
		{
			return annotationEditbox.StringValue;
		}

		void IView.UpdateWorkspaceEditControls(bool enabled, string nameValue, string nameBanner, string nameWarning, string annotationValue)
		{
			idEditbox.Enabled = enabled;
			if (idEditbox.StringValue != nameValue)
				idEditbox.StringValue = nameValue;
			idEditbox.Cell.PlaceholderString = nameBanner;
			annotationEditbox.Enabled = enabled;
			if (annotationEditbox.StringValue != annotationValue)
				annotationEditbox.StringValue = annotationValue;
			nameWarningIcon.Hidden = nameWarning == null;
			nameWarningIcon.ToolTip = nameWarning ?? "";
		}

		partial void cancelClicked (NSObject sender)
		{
			Window.Close();
		}

		partial void OnUploadClicked (NSObject sender)
		{
			eventsHandler.OnUploadButtonClicked();
		}

		partial void copyUrlButtonClicked (NSObject sender)
		{
			eventsHandler.OnCopyUrlClicked();
		}
	}
}

