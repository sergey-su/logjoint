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
	[Register ("NetworkCredentialsSheet")]
	partial class NetworkCredentialsSheet
	{
		[Outlet]
		MonoMac.AppKit.NSTextField captionLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSecureTextField passwordTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField userNameTextField { get; set; }

		[Outlet]
		LogJoint.UI.NetworkCredentialsSheet window { get; set; }

		[Action ("foo:")]
		partial void foo (MonoMac.Foundation.NSObject sender);

		[Action ("OnCancelled:")]
		partial void OnCancelled (MonoMac.Foundation.NSObject sender);

		[Action ("OnConfirmed:")]
		partial void OnConfirmed (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (captionLabel != null) {
				captionLabel.Dispose ();
				captionLabel = null;
			}

			if (passwordTextField != null) {
				passwordTextField.Dispose ();
				passwordTextField = null;
			}

			if (userNameTextField != null) {
				userNameTextField.Dispose ();
				userNameTextField = null;
			}

			if (window != null) {
				window.Dispose ();
				window = null;
			}
		}
	}
}
