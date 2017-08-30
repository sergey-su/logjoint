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
	[Register ("FormatDeletionConfirmationPageController")]
	partial class FormatDeletionConfirmationPageController
	{
		[Outlet]
		AppKit.NSTextField dateTextBox { get; set; }

		[Outlet]
		AppKit.NSTextField descriptionTextBox { get; set; }

		[Outlet]
		AppKit.NSTextField fileNameTextBox { get; set; }

		[Outlet]
		AppKit.NSTextField messageLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (messageLabel != null) {
				messageLabel.Dispose ();
				messageLabel = null;
			}

			if (descriptionTextBox != null) {
				descriptionTextBox.Dispose ();
				descriptionTextBox = null;
			}

			if (fileNameTextBox != null) {
				fileNameTextBox.Dispose ();
				fileNameTextBox = null;
			}

			if (dateTextBox != null) {
				dateTextBox.Dispose ();
				dateTextBox = null;
			}
		}
	}
}
