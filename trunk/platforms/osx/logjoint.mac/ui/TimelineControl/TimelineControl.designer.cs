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
	[Register ("TimelineControlAdapter")]
	partial class TimelineControlAdapter
	{
		[Outlet]
		LogJoint.UI.NSCustomizableView timelineView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (timelineView != null) {
				timelineView.Dispose ();
				timelineView = null;
			}
		}
	}

	[Register ("TimelineControl")]
	partial class TimelineControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
