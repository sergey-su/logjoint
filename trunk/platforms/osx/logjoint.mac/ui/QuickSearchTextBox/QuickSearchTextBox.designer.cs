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
	[Register ("QuickSearchTextBoxAdapter")]
	partial class QuickSearchTextBoxAdapter
	{
		[Outlet]
		AppKit.NSButton dropDownButton { get; set; }

		[Outlet]
		LogJoint.UI.QuickSearchTextBox searchField { get; set; }

		[Outlet]
		AppKit.NSLayoutConstraint trailingConstraint { get; set; }

		[Action ("dropDownButtonClicked:")]
		partial void dropDownButtonClicked (Foundation.NSObject sender);

		[Action ("OnSearchAction:")]
		partial void OnSearchAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (searchField != null) {
				searchField.Dispose ();
				searchField = null;
			}

			if (trailingConstraint != null) {
				trailingConstraint.Dispose ();
				trailingConstraint = null;
			}

			if (dropDownButton != null) {
				dropDownButton.Dispose ();
				dropDownButton = null;
			}
		}
	}

	[Register ("QuickSearchTextBox")]
	partial class QuickSearchTextBox
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
