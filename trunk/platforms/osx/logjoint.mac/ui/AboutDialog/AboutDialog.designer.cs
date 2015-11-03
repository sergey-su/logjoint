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
	[Register ("AboutDialogAdapter")]
	partial class AboutDialogAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSButton copyMacLinkButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton copyWinLinkButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField feedbackLebel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel feedbackLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField macInstallerLinkField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField macInstallerText { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField shareLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField textField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField winInstallerLinkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField winInstallerText { get; set; }

		[Action ("OnCopyMacInstallerClicked:")]
		partial void OnCopyMacInstallerClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnCopyWinInstallerClicked:")]
		partial void OnCopyWinInstallerClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (macInstallerLinkField != null) {
				macInstallerLinkField.Dispose ();
				macInstallerLinkField = null;
			}

			if (macInstallerText != null) {
				macInstallerText.Dispose ();
				macInstallerText = null;
			}

			if (shareLabel != null) {
				shareLabel.Dispose ();
				shareLabel = null;
			}

			if (textField != null) {
				textField.Dispose ();
				textField = null;
			}

			if (winInstallerLinkLabel != null) {
				winInstallerLinkLabel.Dispose ();
				winInstallerLinkLabel = null;
			}

			if (winInstallerText != null) {
				winInstallerText.Dispose ();
				winInstallerText = null;
			}

			if (copyMacLinkButton != null) {
				copyMacLinkButton.Dispose ();
				copyMacLinkButton = null;
			}

			if (copyWinLinkButton != null) {
				copyWinLinkButton.Dispose ();
				copyWinLinkButton = null;
			}

			if (feedbackLebel != null) {
				feedbackLebel.Dispose ();
				feedbackLebel = null;
			}

			if (feedbackLinkLabel != null) {
				feedbackLinkLabel.Dispose ();
				feedbackLinkLabel = null;
			}
		}
	}

	[Register ("AboutDialog")]
	partial class AboutDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
