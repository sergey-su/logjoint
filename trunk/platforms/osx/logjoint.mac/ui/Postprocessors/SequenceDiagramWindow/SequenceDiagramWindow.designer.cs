// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	[Register ("SequenceDiagramWindowController")]
	partial class SequenceDiagramWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSButton activeNotificationsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel arrowDetailsLink { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField arrowNameTextField { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView arrowsView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton collapseResponsesCheckbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton collapseRoleInstancesCheckbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScroller horzScroller { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView leftPanelView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView quickSearchPlaceholder { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView rolesCaptionsView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView tagsViewPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScroller vertScroller { get; set; }

		[Action ("OnActiveNotificationButtonClicked:")]
		partial void OnActiveNotificationButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnCollapseResponsesClicked:")]
		partial void OnCollapseResponsesClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnCollapseRoleInstancesClicked:")]
		partial void OnCollapseRoleInstancesClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnCurrentTimeClicked:")]
		partial void OnCurrentTimeClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnNextBookmarkClicked:")]
		partial void OnNextBookmarkClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnNextUserActionClicked:")]
		partial void OnNextUserActionClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnPrevBookmarkClicked:")]
		partial void OnPrevBookmarkClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnPrevUserActionClicked:")]
		partial void OnPrevUserActionClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (arrowDetailsLink != null) {
				arrowDetailsLink.Dispose ();
				arrowDetailsLink = null;
			}

			if (arrowNameTextField != null) {
				arrowNameTextField.Dispose ();
				arrowNameTextField = null;
			}

			if (arrowsView != null) {
				arrowsView.Dispose ();
				arrowsView = null;
			}

			if (collapseResponsesCheckbox != null) {
				collapseResponsesCheckbox.Dispose ();
				collapseResponsesCheckbox = null;
			}

			if (collapseRoleInstancesCheckbox != null) {
				collapseRoleInstancesCheckbox.Dispose ();
				collapseRoleInstancesCheckbox = null;
			}

			if (horzScroller != null) {
				horzScroller.Dispose ();
				horzScroller = null;
			}

			if (leftPanelView != null) {
				leftPanelView.Dispose ();
				leftPanelView = null;
			}

			if (quickSearchPlaceholder != null) {
				quickSearchPlaceholder.Dispose ();
				quickSearchPlaceholder = null;
			}

			if (rolesCaptionsView != null) {
				rolesCaptionsView.Dispose ();
				rolesCaptionsView = null;
			}

			if (tagsViewPlaceholder != null) {
				tagsViewPlaceholder.Dispose ();
				tagsViewPlaceholder = null;
			}

			if (vertScroller != null) {
				vertScroller.Dispose ();
				vertScroller = null;
			}

			if (activeNotificationsButton != null) {
				activeNotificationsButton.Dispose ();
				activeNotificationsButton = null;
			}
		}
	}

	[Register ("SequenceDiagramWindow")]
	partial class SequenceDiagramWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
