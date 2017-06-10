// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("ToastNotificationsViewAdapter")]
	partial class ToastNotificationsViewAdapter
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel link1 { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel link2 { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel link3 { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel link4 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progress1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progress2 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progress3 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progress4 { get; set; }

		[Outlet]
		AppKit.NSButton suppressBtn1 { get; set; }

		[Outlet]
		AppKit.NSButton suppressBtn2 { get; set; }

		[Outlet]
		AppKit.NSButton suppressBtn3 { get; set; }

		[Outlet]
		AppKit.NSButton suppressBtn4 { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView view1 { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView view2 { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView view3 { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView view4 { get; set; }

		[Action ("suppressBtnClicked:")]
		partial void suppressBtnClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (link1 != null) {
				link1.Dispose ();
				link1 = null;
			}

			if (link2 != null) {
				link2.Dispose ();
				link2 = null;
			}

			if (link3 != null) {
				link3.Dispose ();
				link3 = null;
			}

			if (link4 != null) {
				link4.Dispose ();
				link4 = null;
			}

			if (progress1 != null) {
				progress1.Dispose ();
				progress1 = null;
			}

			if (progress2 != null) {
				progress2.Dispose ();
				progress2 = null;
			}

			if (progress3 != null) {
				progress3.Dispose ();
				progress3 = null;
			}

			if (progress4 != null) {
				progress4.Dispose ();
				progress4 = null;
			}

			if (suppressBtn1 != null) {
				suppressBtn1.Dispose ();
				suppressBtn1 = null;
			}

			if (suppressBtn2 != null) {
				suppressBtn2.Dispose ();
				suppressBtn2 = null;
			}

			if (suppressBtn3 != null) {
				suppressBtn3.Dispose ();
				suppressBtn3 = null;
			}

			if (suppressBtn4 != null) {
				suppressBtn4.Dispose ();
				suppressBtn4 = null;
			}

			if (view4 != null) {
				view4.Dispose ();
				view4 = null;
			}

			if (view3 != null) {
				view3.Dispose ();
				view3 = null;
			}

			if (view2 != null) {
				view2.Dispose ();
				view2 = null;
			}

			if (view1 != null) {
				view1.Dispose ();
				view1 = null;
			}
		}
	}

	[Register ("ToastNotificationsView")]
	partial class ToastNotificationsView
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
