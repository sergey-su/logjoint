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
	[Register ("RegexBasedFormatPageController")]
	partial class RegexBasedFormatPageController
	{
		[Outlet]
		AppKit.NSTextField bodyReStatusLabel { get; set; }

		[Outlet]
		AppKit.NSTextField fieldsMappingLabel { get; set; }

		[Outlet]
		AppKit.NSTextField headerReStatusLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel helpLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField sampleLogStatusLabel { get; set; }

		[Outlet]
		AppKit.NSTextField testStatusLabel { get; set; }

		[Action ("OnEditBodyReClicked:")]
		partial void OnEditBodyReClicked (Foundation.NSObject sender);

		[Action ("OnEditFieldsMappingClicked:")]
		partial void OnEditFieldsMappingClicked (Foundation.NSObject sender);

		[Action ("OnEditHeaderReClicked:")]
		partial void OnEditHeaderReClicked (Foundation.NSObject sender);

		[Action ("OnSelectSampleLogClicked:")]
		partial void OnSelectSampleLogClicked (Foundation.NSObject sender);

		[Action ("OnTestClicked:")]
		partial void OnTestClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (headerReStatusLabel != null) {
				headerReStatusLabel.Dispose ();
				headerReStatusLabel = null;
			}

			if (bodyReStatusLabel != null) {
				bodyReStatusLabel.Dispose ();
				bodyReStatusLabel = null;
			}

			if (fieldsMappingLabel != null) {
				fieldsMappingLabel.Dispose ();
				fieldsMappingLabel = null;
			}

			if (testStatusLabel != null) {
				testStatusLabel.Dispose ();
				testStatusLabel = null;
			}

			if (sampleLogStatusLabel != null) {
				sampleLogStatusLabel.Dispose ();
				sampleLogStatusLabel = null;
			}

			if (helpLinkLabel != null) {
				helpLinkLabel.Dispose ();
				helpLinkLabel = null;
			}
		}
	}
}
