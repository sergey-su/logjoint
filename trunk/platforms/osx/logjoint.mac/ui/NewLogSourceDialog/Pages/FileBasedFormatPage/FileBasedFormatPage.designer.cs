// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("FileBasedFormatPageController")]
	partial class FileBasedFormatPageController
	{
		[Outlet]
		AppKit.NSButton browseFileButton { get; set; }

		[Outlet]
		AppKit.NSButton browseFolderButton { get; set; }

		[Outlet]
		AppKit.NSTextField fileTextField { get; set; }

		[Outlet]
		AppKit.NSTextField folderTextField { get; set; }

		[Outlet]
		AppKit.NSButton independentLogsModeButton { get; set; }

		[Outlet]
		AppKit.NSButton rotatedLogModeButton { get; set; }

		[Action ("OnBrowseFileButtonClicked:")]
		partial void OnBrowseFileButtonClicked (Foundation.NSObject sender);

		[Action ("OnBrowseFolderButtonClicked:")]
		partial void OnBrowseFolderButtonClicked (Foundation.NSObject sender);

		[Action ("OnModeSelectionChanged:")]
		partial void OnModeSelectionChanged (Foundation.NSObject sender);
		
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
