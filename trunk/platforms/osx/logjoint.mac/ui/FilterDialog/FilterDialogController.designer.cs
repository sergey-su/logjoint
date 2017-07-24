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
	[Register ("FilterDialogController")]
	partial class FilterDialogController
	{
		[Outlet]
		AppKit.NSPopUpButton actionComboxBox { get; set; }

		[Outlet]
		AppKit.NSButton enabledCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton matchCaseCheckbox { get; set; }

		[Outlet]
		AppKit.NSTextField nameTextBox { get; set; }

		[Outlet]
		AppKit.NSButton regexCheckbox { get; set; }

		[Outlet]
		AppKit.NSTextField templateEditBox { get; set; }

		[Outlet]
		AppKit.NSButton wholeWordCheckbox { get; set; }

		[Action ("OnCancelled:")]
		partial void OnCancelled (Foundation.NSObject sender);

		[Action ("OnConfirmed:")]
		partial void OnConfirmed (Foundation.NSObject sender);

		[Action ("OnInputChanged:")]
		partial void OnInputChanged (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (nameTextBox != null) {
				nameTextBox.Dispose ();
				nameTextBox = null;
			}

			if (enabledCheckbox != null) {
				enabledCheckbox.Dispose ();
				enabledCheckbox = null;
			}

			if (templateEditBox != null) {
				templateEditBox.Dispose ();
				templateEditBox = null;
			}

			if (matchCaseCheckbox != null) {
				matchCaseCheckbox.Dispose ();
				matchCaseCheckbox = null;
			}

			if (regexCheckbox != null) {
				regexCheckbox.Dispose ();
				regexCheckbox = null;
			}

			if (wholeWordCheckbox != null) {
				wholeWordCheckbox.Dispose ();
				wholeWordCheckbox = null;
			}

			if (actionComboxBox != null) {
				actionComboxBox.Dispose ();
				actionComboxBox = null;
			}
		}
	}
}
