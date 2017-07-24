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
		AppKit.NSView listPlaceholder { get; set; }

		[Action ("OnAddFilterClicked:")]
		partial void OnAddFilterClicked (Foundation.NSObject sender);

		[Action ("OnDeleteFilterClicked:")]
		partial void OnDeleteFilterClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (listPlaceholder != null) {
				listPlaceholder.Dispose ();
				listPlaceholder = null;
			}
		}
	}
}
