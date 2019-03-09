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
	[Register ("TagsSelectionSheetController")]
	partial class TagsSelectionSheetController
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel formulaEditLinkLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel formulaLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextView formulaTextView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel linkLabel { get; set; }

		[Outlet]
		AppKit.NSButton okButton { get; set; }

		[Outlet]
		AppKit.NSScrollView suggestionsContainer { get; set; }

		[Outlet]
		AppKit.NSTextField suggestionsLabel { get; set; }

		[Outlet]
		AppKit.NSView suggestionsView { get; set; }

		[Outlet]
		AppKit.NSTableView table { get; set; }

		[Outlet]
		AppKit.NSTabView tabView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel tagsStatusLinkLabel { get; set; }

		[Action ("OnCancelled:")]
		partial void OnCancelled (Foundation.NSObject sender);

		[Action ("OnConfirmed:")]
		partial void OnConfirmed (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (formulaEditLinkLabel != null) {
				formulaEditLinkLabel.Dispose ();
				formulaEditLinkLabel = null;
			}

			if (formulaLinkLabel != null) {
				formulaLinkLabel.Dispose ();
				formulaLinkLabel = null;
			}

			if (formulaTextView != null) {
				formulaTextView.Dispose ();
				formulaTextView = null;
			}

			if (linkLabel != null) {
				linkLabel.Dispose ();
				linkLabel = null;
			}

			if (okButton != null) {
				okButton.Dispose ();
				okButton = null;
			}

			if (suggestionsView != null) {
				suggestionsView.Dispose ();
				suggestionsView = null;
			}

			if (table != null) {
				table.Dispose ();
				table = null;
			}

			if (tabView != null) {
				tabView.Dispose ();
				tabView = null;
			}

			if (tagsStatusLinkLabel != null) {
				tagsStatusLinkLabel.Dispose ();
				tagsStatusLinkLabel = null;
			}

			if (suggestionsContainer != null) {
				suggestionsContainer.Dispose ();
				suggestionsContainer = null;
			}

			if (suggestionsLabel != null) {
				suggestionsLabel.Dispose ();
				suggestionsLabel = null;
			}
		}
	}

	[Register ("TagsSelectionSheet")]
	partial class TagsSelectionSheet
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
