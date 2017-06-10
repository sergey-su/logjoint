// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SourcePropertiesDialogAdapter")]
	partial class SourcePropertiesDialogAdapter
	{
		[Outlet]
		AppKit.NSTextField annotationEditBox { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel changeColorLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField colorPanel { get; set; }

		[Outlet]
		AppKit.NSMenu colorsMenu { get; set; }

		[Outlet]
		AppKit.NSButton copyPathButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel firstMessageLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField formatTextField { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel lastMessageLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField loadedMessagesLabel { get; set; }

		[Outlet]
		AppKit.NSButton loadedMessagesWarningIcon { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel loadedMessagesWarningLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField nameTextField { get; set; }

		[Outlet]
		AppKit.NSButton openContainingFolderButton { get; set; }

		[Outlet]
		AppKit.NSButton saveAsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel stateDetailsLink { get; set; }

		[Outlet]
		AppKit.NSTextField stateLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel suspendResumeTrackingLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField timeShiftTextField { get; set; }

		[Outlet]
		AppKit.NSTextField trackChangesLabel { get; set; }

		[Outlet]
		AppKit.NSButton visibleCheckbox { get; set; }

		[Action ("OnCloseButtonClicked:")]
		partial void OnCloseButtonClicked (Foundation.NSObject sender);

		[Action ("OnColorItemClicked:")]
		partial void OnColorItemClicked (Foundation.NSObject sender);

		[Action ("OnCopyButtonClicked:")]
		partial void OnCopyButtonClicked (Foundation.NSObject sender);

		[Action ("OnOpenContainingFolderButtonClicked:")]
		partial void OnOpenContainingFolderButtonClicked (Foundation.NSObject sender);

		[Action ("OnSaveButtonClicked:")]
		partial void OnSaveButtonClicked (Foundation.NSObject sender);

		[Action ("OnVisibleCheckboxClicked:")]
		partial void OnVisibleCheckboxClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (annotationEditBox != null) {
				annotationEditBox.Dispose ();
				annotationEditBox = null;
			}

			if (changeColorLinkLabel != null) {
				changeColorLinkLabel.Dispose ();
				changeColorLinkLabel = null;
			}

			if (colorPanel != null) {
				colorPanel.Dispose ();
				colorPanel = null;
			}

			if (colorsMenu != null) {
				colorsMenu.Dispose ();
				colorsMenu = null;
			}

			if (copyPathButton != null) {
				copyPathButton.Dispose ();
				copyPathButton = null;
			}

			if (firstMessageLinkLabel != null) {
				firstMessageLinkLabel.Dispose ();
				firstMessageLinkLabel = null;
			}

			if (formatTextField != null) {
				formatTextField.Dispose ();
				formatTextField = null;
			}

			if (lastMessageLinkLabel != null) {
				lastMessageLinkLabel.Dispose ();
				lastMessageLinkLabel = null;
			}

			if (loadedMessagesLabel != null) {
				loadedMessagesLabel.Dispose ();
				loadedMessagesLabel = null;
			}

			if (loadedMessagesWarningIcon != null) {
				loadedMessagesWarningIcon.Dispose ();
				loadedMessagesWarningIcon = null;
			}

			if (loadedMessagesWarningLinkLabel != null) {
				loadedMessagesWarningLinkLabel.Dispose ();
				loadedMessagesWarningLinkLabel = null;
			}

			if (nameTextField != null) {
				nameTextField.Dispose ();
				nameTextField = null;
			}

			if (saveAsButton != null) {
				saveAsButton.Dispose ();
				saveAsButton = null;
			}

			if (stateDetailsLink != null) {
				stateDetailsLink.Dispose ();
				stateDetailsLink = null;
			}

			if (stateLabel != null) {
				stateLabel.Dispose ();
				stateLabel = null;
			}

			if (suspendResumeTrackingLinkLabel != null) {
				suspendResumeTrackingLinkLabel.Dispose ();
				suspendResumeTrackingLinkLabel = null;
			}

			if (timeShiftTextField != null) {
				timeShiftTextField.Dispose ();
				timeShiftTextField = null;
			}

			if (trackChangesLabel != null) {
				trackChangesLabel.Dispose ();
				trackChangesLabel = null;
			}

			if (visibleCheckbox != null) {
				visibleCheckbox.Dispose ();
				visibleCheckbox = null;
			}

			if (openContainingFolderButton != null) {
				openContainingFolderButton.Dispose ();
				openContainingFolderButton = null;
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
