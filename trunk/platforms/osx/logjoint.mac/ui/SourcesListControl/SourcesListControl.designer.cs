// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SourcesListControl")]
	partial class SourcesListControl
	{
		[Outlet]
		MonoMac.AppKit.NSOutlineView outlineView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn sourceCheckedColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn sourceDescriptionColumn { get; set; }

		[Outlet]
		LogJoint.UI.SourcesListControl view { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (outlineView != null) {
				outlineView.Dispose ();
				outlineView = null;
			}

			if (sourceDescriptionColumn != null) {
				sourceDescriptionColumn.Dispose ();
				sourceDescriptionColumn = null;
			}

			if (view != null) {
				view.Dispose ();
				view = null;
			}

			if (sourceCheckedColumn != null) {
				sourceCheckedColumn.Dispose ();
				sourceCheckedColumn = null;
			}
		}
	}
}
