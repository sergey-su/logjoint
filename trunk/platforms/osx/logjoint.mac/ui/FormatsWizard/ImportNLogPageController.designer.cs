// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("ImportNLogPageController")]
	partial class ImportNLogPageController
	{
		[Outlet]
		AppKit.NSTextField configFileTextBox { get; set; }

		[Outlet]
		AppKit.NSTableView patternsTable { get; set; }

		[Outlet]
		AppKit.NSTextField patternTextBox { get; set; }

		[Action ("OnOpenFileClicked:")]
		partial void OnOpenFileClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (patternsTable != null) {
				patternsTable.Dispose ();
				patternsTable = null;
			}

			if (patternTextBox != null) {
				patternTextBox.Dispose ();
				patternTextBox = null;
			}

			if (configFileTextBox != null) {
				configFileTextBox.Dispose ();
				configFileTextBox = null;
			}
		}
	}
}
