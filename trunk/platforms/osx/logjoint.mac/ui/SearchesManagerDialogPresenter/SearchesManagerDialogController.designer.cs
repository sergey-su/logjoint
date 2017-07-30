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
	[Register ("SearchesManagerDialogController")]
	partial class SearchesManagerDialogController
	{
		[Outlet]
		AppKit.NSButton addButton { get; set; }

		[Outlet]
		AppKit.NSButton exportButton { get; set; }

		[Outlet]
		AppKit.NSButton importButton { get; set; }

		[Outlet]
		AppKit.NSOutlineView outlineView { get; set; }

		[Outlet]
		AppKit.NSButton propertiesButton { get; set; }

		[Outlet]
		AppKit.NSButton removeButton { get; set; }

		[Action ("OnAddClicked:")]
		partial void OnAddClicked (Foundation.NSObject sender);

		[Action ("OnCloseClicked:")]
		partial void OnCloseClicked (Foundation.NSObject sender);

		[Action ("OnExportClicked:")]
		partial void OnExportClicked (Foundation.NSObject sender);

		[Action ("OnImportClicked:")]
		partial void OnImportClicked (Foundation.NSObject sender);

		[Action ("OnPropertiesClicked:")]
		partial void OnPropertiesClicked (Foundation.NSObject sender);

		[Action ("OnRemoveClicked:")]
		partial void OnRemoveClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (importButton != null) {
				importButton.Dispose ();
				importButton = null;
			}

			if (addButton != null) {
				addButton.Dispose ();
				addButton = null;
			}

			if (removeButton != null) {
				removeButton.Dispose ();
				removeButton = null;
			}

			if (propertiesButton != null) {
				propertiesButton.Dispose ();
				propertiesButton = null;
			}

			if (exportButton != null) {
				exportButton.Dispose ();
				exportButton = null;
			}

			if (outlineView != null) {
				outlineView.Dispose ();
				outlineView = null;
			}
		}
	}
}
