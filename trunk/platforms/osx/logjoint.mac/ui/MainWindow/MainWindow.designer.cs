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
	[Register ("MainWindowController")]
	partial class MainWindowAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSView bookmarksManagementViewPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView loadedMessagesPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbar mainToolbar { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbarItem pendingUpdateNotificationButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView rootView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView searchPanelViewPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView searchResultsPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSplitView searchResultsSplitter { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbarItem shareToolbarItem { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView sourcesManagementViewPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView statusPopupPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSToolbarItem stopLongOpButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton stopLongOperationButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTabView tabView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSegmentedControl toolbarTabsSelector { get; set; }

		[Action ("OnCurrentTabSelected:")]
		partial void OnCurrentTabSelected (MonoMac.Foundation.NSObject sender);

		[Action ("OnRestartButtonClicked:")]
		partial void OnRestartButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnStopLongOpButtonPressed:")]
		partial void OnStopLongOpButtonPressed (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
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

			if (toolbarTabsSelector != null) {
				toolbarTabsSelector.Dispose ();
				toolbarTabsSelector = null;
			}

			if (statusPopupPlaceholder != null) {
				statusPopupPlaceholder.Dispose ();
				statusPopupPlaceholder = null;
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
