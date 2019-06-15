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
	[Register ("MessagePropertiesDialogAdapter")]
	partial class MessagePropertiesDialogAdapter
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel bookmarkActionLabel { get; set; }

		[Outlet]
		AppKit.NSTextField bookmarkStatusLabel { get; set; }

		[Outlet]
		AppKit.NSSegmentedControl contentModeSegmentedControl { get; set; }

		[Outlet]
		AppKit.NSButton hlCheckbox { get; set; }

		[Outlet]
		AppKit.NSTextField severityLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel sourceLabel { get; set; }

		[Outlet]
		AppKit.NSTextView textView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel threadLabel { get; set; }

		[Outlet]
		AppKit.NSTextField timestampLabel { get; set; }

		[Action ("onCloseClicked:")]
		partial void onCloseClicked (Foundation.NSObject sender);

		[Action ("onNextMessageCliecked:")]
		partial void onNextMessageCliecked (Foundation.NSObject sender);

		[Action ("onPrevMessageClicked:")]
		partial void onPrevMessageClicked (Foundation.NSObject sender);

		[Action ("onViewModeChanged:")]
		partial void onViewModeChanged (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (bookmarkActionLabel != null) {
				bookmarkActionLabel.Dispose ();
				bookmarkActionLabel = null;
			}

			if (bookmarkStatusLabel != null) {
				bookmarkStatusLabel.Dispose ();
				bookmarkStatusLabel = null;
			}

			if (contentModeSegmentedControl != null) {
				contentModeSegmentedControl.Dispose ();
				contentModeSegmentedControl = null;
			}

			if (hlCheckbox != null) {
				hlCheckbox.Dispose ();
				hlCheckbox = null;
			}

			if (severityLabel != null) {
				severityLabel.Dispose ();
				severityLabel = null;
			}

			if (sourceLabel != null) {
				sourceLabel.Dispose ();
				sourceLabel = null;
			}

			if (textView != null) {
				textView.Dispose ();
				textView = null;
			}

			if (threadLabel != null) {
				threadLabel.Dispose ();
				threadLabel = null;
			}

			if (timestampLabel != null) {
				timestampLabel.Dispose ();
				timestampLabel = null;
			}
		}
	}

	[Register ("MessagePropertiesDialog")]
	partial class MessagePropertiesDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
