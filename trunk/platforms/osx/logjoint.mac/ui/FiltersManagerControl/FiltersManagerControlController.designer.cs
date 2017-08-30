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
		AppKit.NSButton enableFilteringButton { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel link1 { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel link2 { get; set; }

		[Outlet]
		AppKit.NSView listPlaceholder { get; set; }

		[Outlet]
		AppKit.NSLayoutConstraint listTopConstraint { get; set; }

		[Outlet]
		AppKit.NSButton optionsButton { get; set; }

		[Outlet]
		AppKit.NSButton removeFilterButton { get; set; }

		[Action ("OnAddFilterClicked:")]
		partial void OnAddFilterClicked (Foundation.NSObject sender);

		[Action ("OnDeleteFilterClicked:")]
		partial void OnDeleteFilterClicked (Foundation.NSObject sender);

		[Action ("OnEnableFilteringClicked:")]
		partial void OnEnableFilteringClicked (Foundation.NSObject sender);

		[Action ("OnOptionsButtonClicked:")]
		partial void OnOptionsButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (addFilterButton != null) {
				addFilterButton.Dispose ();
				addFilterButton = null;
			}

			if (listPlaceholder != null) {
				listPlaceholder.Dispose ();
				listPlaceholder = null;
			}

			if (removeFilterButton != null) {
				removeFilterButton.Dispose ();
				removeFilterButton = null;
			}

			if (listTopConstraint != null) {
				listTopConstraint.Dispose ();
				listTopConstraint = null;
			}

			if (optionsButton != null) {
				optionsButton.Dispose ();
				optionsButton = null;
			}

			if (enableFilteringButton != null) {
				enableFilteringButton.Dispose ();
				enableFilteringButton = null;
			}

			if (link1 != null) {
				link1.Dispose ();
				link1 = null;
			}

			if (link2 != null) {
				link2.Dispose ();
				link2 = null;
			}
		}
	}
}
