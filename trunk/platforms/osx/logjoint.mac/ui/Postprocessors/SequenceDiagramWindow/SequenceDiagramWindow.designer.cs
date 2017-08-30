// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	[Register ("SequenceDiagramWindowController")]
	partial class SequenceDiagramWindowController
	{
		[Outlet]
		AppKit.NSButton activeNotificationsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel arrowDetailsLink { get; set; }

		[Outlet]
		AppKit.NSTextField arrowNameTextField { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView arrowsView { get; set; }

		[Outlet]
		AppKit.NSButton collapseResponsesCheckbox { get; set; }

		[Outlet]
		AppKit.NSButton collapseRoleInstancesCheckbox { get; set; }

		[Outlet]
		AppKit.NSScroller horzScroller { get; set; }

		[Outlet]
		AppKit.NSLayoutConstraint horzScrollerHeightConstraint { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView leftPanelView { get; set; }

		[Outlet]
		AppKit.NSView quickSearchPlaceholder { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView rolesCaptionsView { get; set; }

		[Outlet]
		AppKit.NSView tagsViewPlaceholder { get; set; }

		[Outlet]
		AppKit.NSScroller vertScroller { get; set; }

		[Action ("OnActiveNotificationButtonClicked:")]
		partial void OnActiveNotificationButtonClicked (Foundation.NSObject sender);

		[Action ("OnCollapseResponsesClicked:")]
		partial void OnCollapseResponsesClicked (Foundation.NSObject sender);

		[Action ("OnCollapseRoleInstancesClicked:")]
		partial void OnCollapseRoleInstancesClicked (Foundation.NSObject sender);

		[Action ("OnCurrentTimeClicked:")]
		partial void OnCurrentTimeClicked (Foundation.NSObject sender);

		[Action ("OnNextBookmarkClicked:")]
		partial void OnNextBookmarkClicked (Foundation.NSObject sender);

		[Action ("OnNextUserActionClicked:")]
		partial void OnNextUserActionClicked (Foundation.NSObject sender);

		[Action ("OnPrevBookmarkClicked:")]
		partial void OnPrevBookmarkClicked (Foundation.NSObject sender);

		[Action ("OnPrevUserActionClicked:")]
		partial void OnPrevUserActionClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (activeNotificationsButton != null) {
				activeNotificationsButton.Dispose ();
				activeNotificationsButton = null;
			}

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

			if (horzScrollerHeightConstraint != null) {
				horzScrollerHeightConstraint.Dispose ();
				horzScrollerHeightConstraint = null;
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
