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
	[Register ("SearchPanelControlAdapter")]
	partial class SearchPanelControlAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSButton matchCaseCheckbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton regexCheckbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSearchField searchTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton wholeWordCheckbox { get; set; }

		[Action ("searchTextBoxEnterPressed:")]
		partial void searchTextBoxEnterPressed (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (matchCaseCheckbox != null) {
				matchCaseCheckbox.Dispose ();
				matchCaseCheckbox = null;
			}

			if (regexCheckbox != null) {
				regexCheckbox.Dispose ();
				regexCheckbox = null;
			}

			if (searchTextField != null) {
				searchTextField.Dispose ();
				searchTextField = null;
			}

			if (wholeWordCheckbox != null) {
				wholeWordCheckbox.Dispose ();
				wholeWordCheckbox = null;
			}
		}
	}
}
