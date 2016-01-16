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
	[Register ("FormatDetectionPageController")]
	partial class FormatDetectionPageController
	{
		[Outlet]
		MonoMac.AppKit.NSTextField fileNameTextField { get; set; }

		[Action ("OnBrowseButtonClicked:")]
		partial void OnBrowseButtonClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fileNameTextField != null) {
				fileNameTextField.Dispose ();
				fileNameTextField = null;
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
