// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SourcePropertiesDialogAdapter")]
	partial class SourcePropertiesDialogAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSTextField annotationEditBox { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel changeColorLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField colorPanel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel firstMessageLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField formatTextField { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel lastMessageLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField loadedMessagesLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton loadedMessagesWarningIcon { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel loadedMessagesWarningLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField nameTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton saveAsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel stateDetailsLink { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField stateLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel suspendResumeTrackingLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField timeShiftTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField trackChangesLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton visibleCheckbox { get; set; }

		[Action ("OnCloseButtonClicked:")]
		partial void OnCloseButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnSaveButtonClicked:")]
		partial void OnSaveButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnVisibleCheckboxClicked:")]
		partial void OnVisibleCheckboxClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (loadedMessagesWarningIcon != null) {
				loadedMessagesWarningIcon.Dispose ();
				loadedMessagesWarningIcon = null;
			}

			if (loadedMessagesWarningLinkLabel != null) {
				loadedMessagesWarningLinkLabel.Dispose ();
				loadedMessagesWarningLinkLabel = null;
			}

			if (lastMessageLinkLabel != null) {
				lastMessageLinkLabel.Dispose ();
				lastMessageLinkLabel = null;
			}

			if (firstMessageLinkLabel != null) {
				firstMessageLinkLabel.Dispose ();
				firstMessageLinkLabel = null;
			}

			if (suspendResumeTrackingLinkLabel != null) {
				suspendResumeTrackingLinkLabel.Dispose ();
				suspendResumeTrackingLinkLabel = null;
			}

			if (trackChangesLabel != null) {
				trackChangesLabel.Dispose ();
				trackChangesLabel = null;
			}

			if (formatTextField != null) {
				formatTextField.Dispose ();
				formatTextField = null;
			}

			if (nameTextField != null) {
				nameTextField.Dispose ();
				nameTextField = null;
			}

			if (visibleCheckbox != null) {
				visibleCheckbox.Dispose ();
				visibleCheckbox = null;
			}

			if (colorPanel != null) {
				colorPanel.Dispose ();
				colorPanel = null;
			}

			if (changeColorLinkLabel != null) {
				changeColorLinkLabel.Dispose ();
				changeColorLinkLabel = null;
			}

			if (annotationEditBox != null) {
				annotationEditBox.Dispose ();
				annotationEditBox = null;
			}

			if (timeShiftTextField != null) {
				timeShiftTextField.Dispose ();
				timeShiftTextField = null;
			}

			if (loadedMessagesLabel != null) {
				loadedMessagesLabel.Dispose ();
				loadedMessagesLabel = null;
			}

			if (saveAsButton != null) {
				saveAsButton.Dispose ();
				saveAsButton = null;
			}

			if (stateLabel != null) {
				stateLabel.Dispose ();
				stateLabel = null;
			}

			if (stateDetailsLink != null) {
				stateDetailsLink.Dispose ();
				stateDetailsLink = null;
			}
		}
	}

	[Register ("SourcePropertiesDialog")]
	partial class SourcePropertiesDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
