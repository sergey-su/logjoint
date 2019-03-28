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
	[Register ("FormatDetectionPageController")]
	partial class FormatDetectionPageController
	{
		[Outlet]
		AppKit.NSTextField fileNameTextField { get; set; }

		[Outlet]
		AppKit.NSTextField keyFileField { get; set; }

		[Action ("OnBrowseButtonClicked:")]
		partial void OnBrowseButtonClicked (Foundation.NSObject sender);

		[Action ("OnBrowseKeyClicked:")]
		partial void OnBrowseKeyClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fileNameTextField != null) {
				fileNameTextField.Dispose ();
				fileNameTextField = null;
			}

			if (keyFileField != null) {
				keyFileField.Dispose ();
				keyFileField = null;
			}
		}
	}

	[Register ("FormatDetectionPage")]
	partial class FormatDetectionPage
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
