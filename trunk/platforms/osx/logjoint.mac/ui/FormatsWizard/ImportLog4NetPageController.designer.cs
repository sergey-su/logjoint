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
	[Register ("ImportLog4NetPageController")]
	partial class ImportLog4NetPageController
	{
		[Outlet]
		AppKit.NSTextField configFileTextField { get; set; }

		[Outlet]
		AppKit.NSTableView patternsTable { get; set; }

		[Outlet]
		AppKit.NSTextField patternTextField { get; set; }

		[Action ("OnOpenFileClicked:")]
		partial void OnOpenFileClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (patternTextField != null) {
				patternTextField.Dispose ();
				patternTextField = null;
			}

			if (configFileTextField != null) {
				configFileTextField.Dispose ();
				configFileTextField = null;
			}

			if (patternsTable != null) {
				patternsTable.Dispose ();
				patternsTable = null;
			}
		}
	}
}
