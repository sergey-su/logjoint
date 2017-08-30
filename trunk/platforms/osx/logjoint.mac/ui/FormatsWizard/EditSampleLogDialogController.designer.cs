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
	[Register ("EditSampleLogDialogController")]
	partial class EditSampleLogDialogController
	{
		[Outlet]
		AppKit.NSTextView textView { get; set; }

		[Action ("OnCancelClicked:")]
		partial void OnCancelClicked (Foundation.NSObject sender);

		[Action ("OnLoadFileClicked:")]
		partial void OnLoadFileClicked (Foundation.NSObject sender);

		[Action ("OnOkClicked:")]
		partial void OnOkClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (textView != null) {
				textView.Dispose ();
				textView = null;
			}
		}
	}
}
