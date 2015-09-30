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
	[Register ("SourcesManagementControl")]
	partial class SourcesManagementControl
	{
		[Outlet]
		MonoMac.AppKit.NSButton deleteSelectedSourcesButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton recentSourcesButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView sourcesListPlaceholder { get; set; }

		[Outlet]
		LogJoint.UI.SourcesManagementControl view { get; set; }

		[Action ("addLogSourceButtonClicked:")]
		partial void addLogSourceButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("deleteSelectedSourcesButtonClicked:")]
		partial void deleteSelectedSourcesButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnRecentSourcesButtonClicked:")]
		partial void OnRecentSourcesButtonClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (deleteSelectedSourcesButton != null) {
				deleteSelectedSourcesButton.Dispose ();
				deleteSelectedSourcesButton = null;
			}

			if (sourcesListPlaceholder != null) {
				sourcesListPlaceholder.Dispose ();
				sourcesListPlaceholder = null;
			}

			if (view != null) {
				view.Dispose ();
				view = null;
			}

			if (recentSourcesButton != null) {
				recentSourcesButton.Dispose ();
				recentSourcesButton = null;
			}
		}
	}
}
