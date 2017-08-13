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
	[Register ("NewLogSourceDialogController")]
	partial class NewLogSourceDialogController
	{
		[Outlet]
		AppKit.NSTextField formatDescriptionLabel { get; set; }

		[Outlet]
		AppKit.NSTextField formatNameLabel { get; set; }

		[Outlet]
		AppKit.NSBox formatOptionsPagePlaceholder { get; set; }

		[Outlet]
		AppKit.NSTableView formatsListTable { get; set; }

		[Action ("OnCancelPressed:")]
		partial void OnCancelPressed (Foundation.NSObject sender);

		[Action ("OnManageClicked:")]
		partial void OnManageClicked (Foundation.NSObject sender);

		[Action ("OnOKPressed:")]
		partial void OnOKPressed (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (formatDescriptionLabel != null) {
				formatDescriptionLabel.Dispose ();
				formatDescriptionLabel = null;
			}

			if (formatNameLabel != null) {
				formatNameLabel.Dispose ();
				formatNameLabel = null;
			}

			if (formatOptionsPagePlaceholder != null) {
				formatOptionsPagePlaceholder.Dispose ();
				formatOptionsPagePlaceholder = null;
			}

			if (formatsListTable != null) {
				formatsListTable.Dispose ();
				formatsListTable = null;
			}
		}
	}

	[Register ("NewLogSourceDialog")]
	partial class NewLogSourceDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
