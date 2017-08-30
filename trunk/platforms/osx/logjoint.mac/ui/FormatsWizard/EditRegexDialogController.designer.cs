// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("EditRegexDialogController")]
	partial class EditRegexDialogController
	{
		[Outlet]
		AppKit.NSTableView capturesTable { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel conceptsLinkLabel { get; set; }

		[Outlet]
		AppKit.NSScrollView emptyReContainer { get; set; }

		[Outlet]
		AppKit.NSTextView emptyReLabel { get; set; }

		[Outlet]
		AppKit.NSScrollView legendContainer { get; set; }

		[Outlet]
		AppKit.NSTextField legendLabel { get; set; }

		[Outlet]
		AppKit.NSTextField matchesCountLabel { get; set; }

		[Outlet]
		AppKit.NSTextField perfRatingLabel { get; set; }

		[Outlet]
		AppKit.NSTextView regexTextBox { get; set; }

		[Outlet]
		AppKit.NSTextField reHelpLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel reHelpLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextView sampleLogTextBox { get; set; }

		[Action ("OnCancelClicked:")]
		partial void OnCancelClicked (Foundation.NSObject sender);

		[Action ("OnOkClicked:")]
		partial void OnOkClicked (Foundation.NSObject sender);

		[Action ("OnTestRegexClicked:")]
		partial void OnTestRegexClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (capturesTable != null) {
				capturesTable.Dispose ();
				capturesTable = null;
			}

			if (conceptsLinkLabel != null) {
				conceptsLinkLabel.Dispose ();
				conceptsLinkLabel = null;
			}

			if (emptyReContainer != null) {
				emptyReContainer.Dispose ();
				emptyReContainer = null;
			}

			if (emptyReLabel != null) {
				emptyReLabel.Dispose ();
				emptyReLabel = null;
			}

			if (matchesCountLabel != null) {
				matchesCountLabel.Dispose ();
				matchesCountLabel = null;
			}

			if (perfRatingLabel != null) {
				perfRatingLabel.Dispose ();
				perfRatingLabel = null;
			}

			if (regexTextBox != null) {
				regexTextBox.Dispose ();
				regexTextBox = null;
			}

			if (reHelpLabel != null) {
				reHelpLabel.Dispose ();
				reHelpLabel = null;
			}

			if (reHelpLinkLabel != null) {
				reHelpLinkLabel.Dispose ();
				reHelpLinkLabel = null;
			}

			if (sampleLogTextBox != null) {
				sampleLogTextBox.Dispose ();
				sampleLogTextBox = null;
			}

			if (legendContainer != null) {
				legendContainer.Dispose ();
				legendContainer = null;
			}

			if (legendLabel != null) {
				legendLabel.Dispose ();
				legendLabel = null;
			}
		}
	}
}
