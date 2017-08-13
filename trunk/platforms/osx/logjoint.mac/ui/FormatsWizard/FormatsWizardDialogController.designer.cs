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
	[Register ("FormatsWizardDialogController")]
	partial class FormatsWizardDialogController
	{
		[Outlet]
		AppKit.NSButton backButton { get; set; }

		[Outlet]
		AppKit.NSButton cancelButton { get; set; }

		[Outlet]
		AppKit.NSButton nextButton { get; set; }

		[Outlet]
		AppKit.NSView pagePlaceholder { get; set; }

		[Action ("backClicked:")]
		partial void backClicked (Foundation.NSObject sender);

		[Action ("cancelClicked:")]
		partial void cancelClicked (Foundation.NSObject sender);

		[Action ("nextClicked:")]
		partial void nextClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (pagePlaceholder != null) {
				pagePlaceholder.Dispose ();
				pagePlaceholder = null;
			}

			if (cancelButton != null) {
				cancelButton.Dispose ();
				cancelButton = null;
			}

			if (backButton != null) {
				backButton.Dispose ();
				backButton = null;
			}

			if (nextButton != null) {
				nextButton.Dispose ();
				nextButton = null;
			}
		}
	}
}
