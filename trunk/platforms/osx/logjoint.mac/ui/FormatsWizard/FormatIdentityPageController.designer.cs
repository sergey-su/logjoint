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
	[Register ("FormatIdentityPageController")]
	partial class FormatIdentityPageController
	{
		[Outlet]
		AppKit.NSTextField companyNameTextField { get; set; }

		[Outlet]
		AppKit.NSTextField descriptionTextField { get; set; }

		[Outlet]
		AppKit.NSTextField formatNameTextField { get; set; }

		[Outlet]
		AppKit.NSTextField headerLebel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (descriptionTextField != null) {
				descriptionTextField.Dispose ();
				descriptionTextField = null;
			}

			if (formatNameTextField != null) {
				formatNameTextField.Dispose ();
				formatNameTextField = null;
			}

			if (headerLebel != null) {
				headerLebel.Dispose ();
				headerLebel = null;
			}

			if (companyNameTextField != null) {
				companyNameTextField.Dispose ();
				companyNameTextField = null;
			}
		}
	}
}
