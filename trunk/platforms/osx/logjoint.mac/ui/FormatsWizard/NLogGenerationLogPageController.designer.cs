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
	[Register ("NLogGenerationLogPageController")]
	partial class NLogGenerationLogPageController
	{
		[Outlet]
		AppKit.NSImageView headerIcon { get; set; }

		[Outlet]
		AppKit.NSTextField headerLabel { get; set; }

		[Outlet]
		AppKit.NSTableColumn iconColumn { get; set; }

		[Outlet]
		AppKit.NSTableView messagesTable { get; set; }

		[Outlet]
		AppKit.NSTextField templateTextBox { get; set; }

		[Outlet]
		AppKit.NSTableColumn textColumn { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (templateTextBox != null) {
				templateTextBox.Dispose ();
				templateTextBox = null;
			}

			if (messagesTable != null) {
				messagesTable.Dispose ();
				messagesTable = null;
			}

			if (textColumn != null) {
				textColumn.Dispose ();
				textColumn = null;
			}

			if (iconColumn != null) {
				iconColumn.Dispose ();
				iconColumn = null;
			}

			if (headerLabel != null) {
				headerLabel.Dispose ();
				headerLabel = null;
			}

			if (headerIcon != null) {
				headerIcon.Dispose ();
				headerIcon = null;
			}
		}
	}
}
