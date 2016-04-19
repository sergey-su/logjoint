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
	[Register ("SharingDialogController")]
	partial class SharingDialogController
	{
		[Outlet]
		MonoMac.AppKit.NSTextField annotationEditbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton cancelButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton copyUrlButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField descriptionLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField idEditbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSImageView nameWarningIcon { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator progressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel statusDetailsLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField statusLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton uploadButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField urlEditbox { get; set; }

		[Outlet]
		MonoMac.AppKit.NSImageView warningSign { get; set; }

		[Action ("cancelClicked:")]
		partial void cancelClicked (MonoMac.Foundation.NSObject sender);

		[Action ("copyUrlButtonClicked:")]
		partial void copyUrlButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnUploadClicked:")]
		partial void OnUploadClicked (MonoMac.Foundation.NSObject sender);

		[Action ("uploadClicked:")]
		partial void uploadClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (annotationEditbox != null) {
				annotationEditbox.Dispose ();
				annotationEditbox = null;
			}

			if (cancelButton != null) {
				cancelButton.Dispose ();
				cancelButton = null;
			}

			if (copyUrlButton != null) {
				copyUrlButton.Dispose ();
				copyUrlButton = null;
			}

			if (descriptionLabel != null) {
				descriptionLabel.Dispose ();
				descriptionLabel = null;
			}

			if (idEditbox != null) {
				idEditbox.Dispose ();
				idEditbox = null;
			}

			if (nameWarningIcon != null) {
				nameWarningIcon.Dispose ();
				nameWarningIcon = null;
			}

			if (progressIndicator != null) {
				progressIndicator.Dispose ();
				progressIndicator = null;
			}

			if (statusDetailsLabel != null) {
				statusDetailsLabel.Dispose ();
				statusDetailsLabel = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}

			if (uploadButton != null) {
				uploadButton.Dispose ();
				uploadButton = null;
			}

			if (urlEditbox != null) {
				urlEditbox.Dispose ();
				urlEditbox = null;
			}

			if (warningSign != null) {
				warningSign.Dispose ();
				warningSign = null;
			}
		}
	}

	[Register ("SharingDialog")]
	partial class SharingDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
