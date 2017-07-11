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
	[Register ("SearchPanelControlAdapter")]
	partial class SearchPanelControlAdapter
	{
		[Outlet]
		AppKit.NSButton fromCurrentPositionCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton matchCaseCheckbox { get; set; }

		[Outlet]
		AppKit.NSView quickSearchPlaceholder { get; set; }

		[Outlet]
		AppKit.NSButton quickSearchRadioButton { get; set; }

		[Outlet]
		AppKit.NSButton regexCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton searchAllRadioButton { get; set; }

		[Outlet]
		AppKit.NSButton searchInCurrentLogCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton searchInCurrentThreadCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton searchInSearchResultsCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton searchUpCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton wholeWordCheckbox { get; set; }

		[Action ("OnSearchModeChanged:")]
		partial void OnSearchModeChanged (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fromCurrentPositionCheckbox != null) {
				fromCurrentPositionCheckbox.Dispose ();
				fromCurrentPositionCheckbox = null;
			}

			if (matchCaseCheckbox != null) {
				matchCaseCheckbox.Dispose ();
				matchCaseCheckbox = null;
			}

			if (quickSearchRadioButton != null) {
				quickSearchRadioButton.Dispose ();
				quickSearchRadioButton = null;
			}

			if (regexCheckbox != null) {
				regexCheckbox.Dispose ();
				regexCheckbox = null;
			}

			if (searchAllRadioButton != null) {
				searchAllRadioButton.Dispose ();
				searchAllRadioButton = null;
			}

			if (searchInCurrentLogCheckbox != null) {
				searchInCurrentLogCheckbox.Dispose ();
				searchInCurrentLogCheckbox = null;
			}

			if (searchInCurrentThreadCheckbox != null) {
				searchInCurrentThreadCheckbox.Dispose ();
				searchInCurrentThreadCheckbox = null;
			}

			if (searchInSearchResultsCheckbox != null) {
				searchInSearchResultsCheckbox.Dispose ();
				searchInSearchResultsCheckbox = null;
			}

			if (searchUpCheckbox != null) {
				searchUpCheckbox.Dispose ();
				searchUpCheckbox = null;
			}

			if (wholeWordCheckbox != null) {
				wholeWordCheckbox.Dispose ();
				wholeWordCheckbox = null;
			}

			if (quickSearchPlaceholder != null) {
				quickSearchPlaceholder.Dispose ();
				quickSearchPlaceholder = null;
			}
		}
	}
}
