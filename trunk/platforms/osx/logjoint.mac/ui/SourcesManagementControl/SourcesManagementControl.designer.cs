// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SourcesManagementControl")]
	partial class SourcesManagementControl
	{
		[Outlet]
		AppKit.NSButton deleteSelectedSourcesButton { get; set; }

		[Outlet]
		AppKit.NSButton logSourcePropertiesButton { get; set; }

		[Outlet]
		AppKit.NSButton recentSourcesButton { get; set; }

		[Outlet]
		AppKit.NSView sourcesListPlaceholder { get; set; }

		[Outlet]
		LogJoint.UI.SourcesManagementControl view { get; set; }

		[Action ("OnAddLogSourceButtonClicked:")]
		partial void OnAddLogSourceButtonClicked (Foundation.NSObject sender);

		[Action ("OnDeleteSelectedSourcesButtonClicked:")]
		partial void OnDeleteSelectedSourcesButtonClicked (Foundation.NSObject sender);

		[Action ("OnLogSourcePropertiesButtonClicked:")]
		partial void OnLogSourcePropertiesButtonClicked (Foundation.NSObject sender);

		[Action ("OnRecentSourcesButtonClicked:")]
		partial void OnRecentSourcesButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (deleteSelectedSourcesButton != null) {
				deleteSelectedSourcesButton.Dispose ();
				deleteSelectedSourcesButton = null;
			}

			if (recentSourcesButton != null) {
				recentSourcesButton.Dispose ();
				recentSourcesButton = null;
			}

			if (sourcesListPlaceholder != null) {
				sourcesListPlaceholder.Dispose ();
				sourcesListPlaceholder = null;
			}

			if (view != null) {
				view.Dispose ();
				view = null;
			}

			if (logSourcePropertiesButton != null) {
				logSourcePropertiesButton.Dispose ();
				logSourcePropertiesButton = null;
			}
		}
	}
}
