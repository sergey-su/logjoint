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
	[Register ("ChooseExistingFormatPageController")]
	partial class ChooseExistingFormatPageController
	{
		[Outlet]
		AppKit.NSButton changeButton { get; set; }

		[Outlet]
		AppKit.NSButton deleteButton { get; set; }

		[Outlet]
		AppKit.NSTableView formatsTable { get; set; }

		[Action ("OnRadioButtonSelected:")]
		partial void OnRadioButtonSelected (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (changeButton != null) {
				changeButton.Dispose ();
				changeButton = null;
			}

			if (deleteButton != null) {
				deleteButton.Dispose ();
				deleteButton = null;
			}

			if (formatsTable != null) {
				formatsTable.Dispose ();
				formatsTable = null;
			}
		}
	}
}
