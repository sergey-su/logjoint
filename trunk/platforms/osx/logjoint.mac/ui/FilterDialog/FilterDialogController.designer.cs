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
		LogJoint.UI.NSLinkLabel nameEditLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField nameTextBox { get; set; }

		[Outlet]
		AppKit.NSButton regexCheckbox { get; set; }

		[Outlet]
		AppKit.NSTextField scopeUnsupportedLabel { get; set; }

		[Outlet]
		AppKit.NSOutlineView scopeView { get; set; }

		[Outlet]
		AppKit.NSScrollView scopeViewContainer { get; set; }

		[Outlet]
		AppKit.NSButton severityCheckbox1 { get; set; }

		[Outlet]
		AppKit.NSButton severityCheckbox2 { get; set; }

		[Outlet]
		AppKit.NSButton severityCheckbox3 { get; set; }

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
			if (actionComboxBox != null) {
				actionComboxBox.Dispose ();
				actionComboxBox = null;
			}

			if (enabledCheckbox != null) {
				enabledCheckbox.Dispose ();
				enabledCheckbox = null;
			}

			if (matchCaseCheckbox != null) {
				matchCaseCheckbox.Dispose ();
				matchCaseCheckbox = null;
			}

			if (nameEditLinkLabel != null) {
				nameEditLinkLabel.Dispose ();
				nameEditLinkLabel = null;
			}

			if (nameTextBox != null) {
				nameTextBox.Dispose ();
				nameTextBox = null;
			}

			if (regexCheckbox != null) {
				regexCheckbox.Dispose ();
				regexCheckbox = null;
			}

			if (severityCheckbox1 != null) {
				severityCheckbox1.Dispose ();
				severityCheckbox1 = null;
			}

			if (severityCheckbox2 != null) {
				severityCheckbox2.Dispose ();
				severityCheckbox2 = null;
			}

			if (severityCheckbox3 != null) {
				severityCheckbox3.Dispose ();
				severityCheckbox3 = null;
			}

			if (templateEditBox != null) {
				templateEditBox.Dispose ();
				templateEditBox = null;
			}

			if (wholeWordCheckbox != null) {
				wholeWordCheckbox.Dispose ();
				wholeWordCheckbox = null;
			}

			if (scopeUnsupportedLabel != null) {
				scopeUnsupportedLabel.Dispose ();
				scopeUnsupportedLabel = null;
			}

			if (scopeView != null) {
				scopeView.Dispose ();
				scopeView = null;
			}

			if (scopeViewContainer != null) {
				scopeViewContainer.Dispose ();
				scopeViewContainer = null;
			}
		}
	}
}
