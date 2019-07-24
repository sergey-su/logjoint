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
	[Register ("OptionsWindowController")]
	partial class OptionsWindowController
	{
		[Outlet]
		AppKit.NSButton pluginActionButton { get; set; }

		[Outlet]
		AppKit.NSTextView pluginDetailsLabel { get; set; }

		[Outlet]
		AppKit.NSTextField pluginHeaderLabel { get; set; }

		[Outlet]
		AppKit.NSTextField pluginsLoadingFailedLabel { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator pluginsLoadingIndicator { get; set; }

		[Outlet]
		AppKit.NSTabViewItem pluginsTab { get; set; }

		[Outlet]
		AppKit.NSTableView pluginsTableView { get; set; }

		[Action ("onCancelButtonClicked:")]
		partial void onCancelButtonClicked (Foundation.NSObject sender);

		[Action ("onOkButtonClicked:")]
		partial void onOkButtonClicked (Foundation.NSObject sender);

		[Action ("pluginActionButtonClicked:")]
		partial void pluginActionButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (pluginActionButton != null) {
				pluginActionButton.Dispose ();
				pluginActionButton = null;
			}

			if (pluginDetailsLabel != null) {
				pluginDetailsLabel.Dispose ();
				pluginDetailsLabel = null;
			}

			if (pluginHeaderLabel != null) {
				pluginHeaderLabel.Dispose ();
				pluginHeaderLabel = null;
			}

			if (pluginsLoadingFailedLabel != null) {
				pluginsLoadingFailedLabel.Dispose ();
				pluginsLoadingFailedLabel = null;
			}

			if (pluginsLoadingIndicator != null) {
				pluginsLoadingIndicator.Dispose ();
				pluginsLoadingIndicator = null;
			}

			if (pluginsTableView != null) {
				pluginsTableView.Dispose ();
				pluginsTableView = null;
			}

			if (pluginsTab != null) {
				pluginsTab.Dispose ();
				pluginsTab = null;
			}
		}
	}
}
