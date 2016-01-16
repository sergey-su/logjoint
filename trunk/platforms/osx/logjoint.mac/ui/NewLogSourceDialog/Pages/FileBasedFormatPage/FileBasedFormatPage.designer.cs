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
	[Register ("FileBasedFormatPageController")]
	partial class FileBasedFormatPageController
	{
		[Outlet]
		MonoMac.AppKit.NSButton browseFileButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton browseFolderButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField fileTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField folderTextField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton independentLogsModeButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton rotatedLogModeButton { get; set; }

		[Action ("OnBrowseFileButtonClicked:")]
		partial void OnBrowseFileButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnBrowseFolderButtonClicked:")]
		partial void OnBrowseFolderButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnModeSelectionChanged:")]
		partial void OnModeSelectionChanged (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (folderTextField != null) {
				folderTextField.Dispose ();
				folderTextField = null;
			}

			if (rotatedLogModeButton != null) {
				rotatedLogModeButton.Dispose ();
				rotatedLogModeButton = null;
			}

			if (independentLogsModeButton != null) {
				independentLogsModeButton.Dispose ();
				independentLogsModeButton = null;
			}

			if (fileTextField != null) {
				fileTextField.Dispose ();
				fileTextField = null;
			}

			if (browseFileButton != null) {
				browseFileButton.Dispose ();
				browseFileButton = null;
			}

			if (browseFolderButton != null) {
				browseFolderButton.Dispose ();
				browseFolderButton = null;
			}
		}
	}

	[Register ("FileBasedFormatPage")]
	partial class FileBasedFormatPage
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
