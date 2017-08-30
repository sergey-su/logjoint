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
	[Register ("SaveFormatPageController")]
	partial class SaveFormatPageController
	{
		[Outlet]
		AppKit.NSTextField fileNameBasisTextBox { get; set; }

		[Outlet]
		AppKit.NSTextField fileNameTextBox { get; set; }

		[Action ("OnFileNameBasisTextBoxChanged:")]
		partial void OnFileNameBasisTextBoxChanged (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fileNameBasisTextBox != null) {
				fileNameBasisTextBox.Dispose ();
				fileNameBasisTextBox = null;
			}

			if (fileNameTextBox != null) {
				fileNameTextBox.Dispose ();
				fileNameTextBox = null;
			}
		}
	}
}
