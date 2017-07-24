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
	[Register ("FiltersManagerControlController")]
	partial class FiltersManagerControlController
	{
		[Outlet]
		AppKit.NSButton addFilterButton { get; set; }

		[Outlet]
		AppKit.NSView listPlaceholder { get; set; }

		[Outlet]
		AppKit.NSButton moveDownButton { get; set; }

		[Outlet]
		AppKit.NSButton moveUpButton { get; set; }

		[Outlet]
		AppKit.NSButton removeFilterButton { get; set; }

		[Action ("OnAddFilterClicked:")]
		partial void OnAddFilterClicked (Foundation.NSObject sender);

		[Action ("OnDeleteFilterClicked:")]
		partial void OnDeleteFilterClicked (Foundation.NSObject sender);

		[Action ("OnMoveDownClicked:")]
		partial void OnMoveDownClicked (Foundation.NSObject sender);

		[Action ("OnMoveUpClicked:")]
		partial void OnMoveUpClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (listPlaceholder != null) {
				listPlaceholder.Dispose ();
				listPlaceholder = null;
			}

			if (addFilterButton != null) {
				addFilterButton.Dispose ();
				addFilterButton = null;
			}

			if (removeFilterButton != null) {
				removeFilterButton.Dispose ();
				removeFilterButton = null;
			}

			if (moveUpButton != null) {
				moveUpButton.Dispose ();
				moveUpButton = null;
			}

			if (moveDownButton != null) {
				moveDownButton.Dispose ();
				moveDownButton = null;
			}
		}
	}
}
