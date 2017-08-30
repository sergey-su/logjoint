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
	[Register ("XsltEditorDialogController")]
	partial class XsltEditorDialogController
	{
		[Outlet]
		AppKit.NSTextView codeTextView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel helpLink { get; set; }

		[Outlet]
		AppKit.NSTextField titleLabel { get; set; }

		[Action ("OnCancelClicked:")]
		partial void OnCancelClicked (Foundation.NSObject sender);

		[Action ("OnOkClicked:")]
		partial void OnOkClicked (Foundation.NSObject sender);

		[Action ("OnTestClicked:")]
		partial void OnTestClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (helpLink != null) {
				helpLink.Dispose ();
				helpLink = null;
			}

			if (titleLabel != null) {
				titleLabel.Dispose ();
				titleLabel = null;
			}

			if (codeTextView != null) {
				codeTextView.Dispose ();
				codeTextView = null;
			}
		}
	}
}
