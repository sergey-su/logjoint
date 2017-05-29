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
	[Register ("TagsListViewController")]
	partial class TagsListViewController
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel linkLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (linkLabel != null) {
				linkLabel.Dispose ();
				linkLabel = null;
			}
		}
	}

	[Register ("TagsListView")]
	partial class TagsListView
	{
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
