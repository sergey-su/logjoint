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
	[Register ("PromptDialogController")]
	partial class PromptDialogController
	{
		[Outlet]
		AppKit.NSTextView contentTextField { get; set; }

		[Outlet]
		AppKit.NSTextField promptLabel { get; set; }

		[Action ("OnAcceptClicked:")]
		partial void OnAcceptClicked (Foundation.NSObject sender);

		[Action ("OnCancelClicked:")]
		partial void OnCancelClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (contentTextField != null) {
				contentTextField.Dispose ();
				contentTextField = null;
			}

			if (promptLabel != null) {
				promptLabel.Dispose ();
				promptLabel = null;
			}
		}
	}

	[Register ("PromptDialog")]
	partial class PromptDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
