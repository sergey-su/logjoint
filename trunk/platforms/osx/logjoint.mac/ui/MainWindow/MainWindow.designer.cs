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
	[Register ("MainWindowController")]
	partial class MainWindowAdapter
	{
		[Outlet]
		AppKit.NSView bookmarksManagementViewPlaceholder { get; set; }

		[Outlet]
		AppKit.NSView highlightingManagementPlaceholder { get; set; }

		[Outlet]
		AppKit.NSView loadedMessagesPlaceholder { get; set; }

		[Outlet]
		AppKit.NSToolbar mainToolbar { get; set; }

		[Outlet]
		AppKit.NSToolbarItem pendingUpdateNotificationButton { get; set; }

		[Outlet]
		AppKit.NSView rootView { get; set; }

		[Outlet]
		AppKit.NSView searchPanelViewPlaceholder { get; set; }

		[Outlet]
		AppKit.NSView searchResultsPlaceholder { get; set; }

		[Outlet]
		AppKit.NSSplitView searchResultsSplitter { get; set; }

		[Outlet]
		AppKit.NSToolbarItem shareToolbarItem { get; set; }

		[Outlet]
		AppKit.NSView sourcesManagementViewPlaceholder { get; set; }

		[Outlet]
		AppKit.NSView statusPopupPlaceholder { get; set; }

		[Outlet]
		AppKit.NSToolbarItem stopLongOpButton { get; set; }

		[Outlet]
		AppKit.NSButton stopLongOperationButton { get; set; }

		[Outlet]
		AppKit.NSTabView tabView { get; set; }

		[Outlet]
		AppKit.NSView timelinePanelPlaceholder { get; set; }

		[Outlet]
		AppKit.NSSplitView timelineSplitter { get; set; }

		[Outlet]
		AppKit.NSSegmentedControl toolbarTabsSelector { get; set; }

		[Action ("OnCurrentTabSelected:")]
		partial void OnCurrentTabSelected (Foundation.NSObject sender);

		[Action ("OnRestartButtonClicked:")]
		partial void OnRestartButtonClicked (Foundation.NSObject sender);

		[Action ("OnShareButtonClicked:")]
		partial void OnShareButtonClicked (Foundation.NSObject sender);

		[Action ("OnStopLongOpButtonPressed:")]
		partial void OnStopLongOpButtonPressed (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (highlightingManagementPlaceholder != null) {
				highlightingManagementPlaceholder.Dispose ();
				highlightingManagementPlaceholder = null;
			}

			if (bookmarksManagementViewPlaceholder != null) {
				bookmarksManagementViewPlaceholder.Dispose ();
				bookmarksManagementViewPlaceholder = null;
			}

			if (loadedMessagesPlaceholder != null) {
				loadedMessagesPlaceholder.Dispose ();
				loadedMessagesPlaceholder = null;
			}

			if (mainToolbar != null) {
				mainToolbar.Dispose ();
				mainToolbar = null;
			}

			if (pendingUpdateNotificationButton != null) {
				pendingUpdateNotificationButton.Dispose ();
				pendingUpdateNotificationButton = null;
			}

			if (rootView != null) {
				rootView.Dispose ();
				rootView = null;
			}

			if (searchPanelViewPlaceholder != null) {
				searchPanelViewPlaceholder.Dispose ();
				searchPanelViewPlaceholder = null;
			}

			if (searchResultsPlaceholder != null) {
				searchResultsPlaceholder.Dispose ();
				searchResultsPlaceholder = null;
			}

			if (searchResultsSplitter != null) {
				searchResultsSplitter.Dispose ();
				searchResultsSplitter = null;
			}

			if (shareToolbarItem != null) {
				shareToolbarItem.Dispose ();
				shareToolbarItem = null;
			}

			if (sourcesManagementViewPlaceholder != null) {
				sourcesManagementViewPlaceholder.Dispose ();
				sourcesManagementViewPlaceholder = null;
			}

			if (statusPopupPlaceholder != null) {
				statusPopupPlaceholder.Dispose ();
				statusPopupPlaceholder = null;
			}

			if (stopLongOpButton != null) {
				stopLongOpButton.Dispose ();
				stopLongOpButton = null;
			}

			if (stopLongOperationButton != null) {
				stopLongOperationButton.Dispose ();
				stopLongOperationButton = null;
			}

			if (tabView != null) {
				tabView.Dispose ();
				tabView = null;
			}

			if (timelinePanelPlaceholder != null) {
				timelinePanelPlaceholder.Dispose ();
				timelinePanelPlaceholder = null;
			}

			if (timelineSplitter != null) {
				timelineSplitter.Dispose ();
				timelineSplitter = null;
			}

			if (toolbarTabsSelector != null) {
				toolbarTabsSelector.Dispose ();
				toolbarTabsSelector = null;
			}
		}
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
