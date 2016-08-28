// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SearchResultsControlAdapter")]
	partial class SearchResultsControlAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSButton closeSearchResultsButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton dropdownButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSLayoutConstraint dropdownHeightConstraint { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView dropdownView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView logViewerPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn pinColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator searchProgress { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField searchResultLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField searchStatusLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton selectCurrentTimeButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView tableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn textColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn visiblityColumn { get; set; }

		[Action ("OnCloseSearchResultsButtonClicked:")]
		partial void OnCloseSearchResultsButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnDropdownButtonClicked:")]
		partial void OnDropdownButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnSelectCurrentTimeClicked:")]
		partial void OnSelectCurrentTimeClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (dropdownHeightConstraint != null) {
				dropdownHeightConstraint.Dispose ();
				dropdownHeightConstraint = null;
			}

			if (dropdownView != null) {
				dropdownView.Dispose ();
				dropdownView = null;
			}

			if (dropdownButton != null) {
				dropdownButton.Dispose ();
				dropdownButton = null;
			}

			if (closeSearchResultsButton != null) {
				closeSearchResultsButton.Dispose ();
				closeSearchResultsButton = null;
			}

			if (logViewerPlaceholder != null) {
				logViewerPlaceholder.Dispose ();
				logViewerPlaceholder = null;
			}

			if (pinColumn != null) {
				pinColumn.Dispose ();
				pinColumn = null;
			}

			if (searchProgress != null) {
				searchProgress.Dispose ();
				searchProgress = null;
			}

			if (searchResultLabel != null) {
				searchResultLabel.Dispose ();
				searchResultLabel = null;
			}

			if (searchStatusLabel != null) {
				searchStatusLabel.Dispose ();
				searchStatusLabel = null;
			}

			if (selectCurrentTimeButton != null) {
				selectCurrentTimeButton.Dispose ();
				selectCurrentTimeButton = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (textColumn != null) {
				textColumn.Dispose ();
				textColumn = null;
			}

			if (visiblityColumn != null) {
				visiblityColumn.Dispose ();
				visiblityColumn = null;
			}
		}
	}

	[Register ("SearchResultsControl")]
	partial class SearchResultsControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
