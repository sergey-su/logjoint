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
	[Register ("SearchEditorDialogController")]
	partial class SearchEditorDialogController
	{
		[Outlet]
		AppKit.NSView filtersManagerViewPlaceholder { get; set; }

		[Outlet]
		AppKit.NSTextField nameTextBox { get; set; }

		[Action ("OnCancelled:")]
		partial void OnCancelled (Foundation.NSObject sender);

		[Action ("OnConfirmed:")]
		partial void OnConfirmed (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (filtersManagerViewPlaceholder != null) {
				filtersManagerViewPlaceholder.Dispose ();
				filtersManagerViewPlaceholder = null;
			}

			if (nameTextBox != null) {
				nameTextBox.Dispose ();
				nameTextBox = null;
			}
		}
	}
}
