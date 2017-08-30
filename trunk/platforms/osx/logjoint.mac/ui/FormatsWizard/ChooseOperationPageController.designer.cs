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
	[Register ("ChooseOperationPageController")]
	partial class ChooseOperationPageController
	{
		[Outlet]
		AppKit.NSButton changeFormatButton { get; set; }

		[Outlet]
		AppKit.NSButton importLog4NetButton { get; set; }

		[Outlet]
		AppKit.NSButton importNLogButton { get; set; }

		[Outlet]
		AppKit.NSButton newREBasedFormatButton { get; set; }

		[Outlet]
		AppKit.NSButton newXMLBasedFormatButton { get; set; }

		[Action ("OnRadioButtonSelected:")]
		partial void OnRadioButtonSelected (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (newXMLBasedFormatButton != null) {
				newXMLBasedFormatButton.Dispose ();
				newXMLBasedFormatButton = null;
			}

			if (changeFormatButton != null) {
				changeFormatButton.Dispose ();
				changeFormatButton = null;
			}

			if (importLog4NetButton != null) {
				importLog4NetButton.Dispose ();
				importLog4NetButton = null;
			}

			if (importNLogButton != null) {
				importNLogButton.Dispose ();
				importNLogButton = null;
			}

			if (newREBasedFormatButton != null) {
				newREBasedFormatButton.Dispose ();
				newREBasedFormatButton = null;
			}
		}
	}
}
