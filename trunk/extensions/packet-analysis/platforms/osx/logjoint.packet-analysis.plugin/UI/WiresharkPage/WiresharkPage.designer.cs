// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.PacketAnalysis.UI
{
	[Register ("WiresharkPageAdapter")]
	partial class WiresharkPageAdapter
	{
		[Outlet]
		AppKit.NSView container { get; set; }

		[Outlet]
		AppKit.NSTextField errorLabel { get; set; }

		[Outlet]
		AppKit.NSTextField fileNameTextField { get; set; }

		[Outlet]
		AppKit.NSTextField keyTextField { get; set; }

		[Action ("OnBrowseButtonClicked:")]
		partial void OnBrowseButtonClicked (Foundation.NSObject sender);

		[Action ("OnBrowseKeyClicked:")]
		partial void OnBrowseKeyClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (fileNameTextField != null) {
				fileNameTextField.Dispose ();
				fileNameTextField = null;
			}

			if (keyTextField != null) {
				keyTextField.Dispose ();
				keyTextField = null;
			}

			if (errorLabel != null) {
				errorLabel.Dispose ();
				errorLabel = null;
			}

			if (container != null) {
				container.Dispose ();
				container = null;
			}
		}
	}

	[Register ("WiresharkPage")]
	partial class WiresharkPage
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
