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
	[Register ("XmlBasedFormatPageController")]
	partial class XmlBasedFormatPageController
	{
		[Outlet]
		AppKit.NSTextField conceptsLabel { get; set; }

		[Outlet]
		AppKit.NSTextField headerReLabel { get; set; }

		[Outlet]
		AppKit.NSTextField headerReStatusLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel helpLinkLabel { get; set; }

		[Outlet]
		AppKit.NSTextField pageTitleLabel { get; set; }

		[Outlet]
		AppKit.NSTextField sampleLogLabel { get; set; }

		[Outlet]
		AppKit.NSTextField sampleLogStatusLabel { get; set; }

		[Outlet]
		AppKit.NSTextField testLabel { get; set; }

		[Outlet]
		AppKit.NSTextField testStatusLabel { get; set; }

		[Outlet]
		AppKit.NSTextField transformLabel { get; set; }

		[Outlet]
		AppKit.NSTextField xsltStatusLabel { get; set; }

		[Action ("OnEditHeaderReClicked:")]
		partial void OnEditHeaderReClicked (Foundation.NSObject sender);

		[Action ("OnEditXsltClicked:")]
		partial void OnEditXsltClicked (Foundation.NSObject sender);

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

			if (helpLinkLabel != null) {
				helpLinkLabel.Dispose ();
				helpLinkLabel = null;
			}

			if (sampleLogStatusLabel != null) {
				sampleLogStatusLabel.Dispose ();
				sampleLogStatusLabel = null;
			}

			if (testStatusLabel != null) {
				testStatusLabel.Dispose ();
				testStatusLabel = null;
			}

			if (xsltStatusLabel != null) {
				xsltStatusLabel.Dispose ();
				xsltStatusLabel = null;
			}

			if (pageTitleLabel != null) {
				pageTitleLabel.Dispose ();
				pageTitleLabel = null;
			}

			if (conceptsLabel != null) {
				conceptsLabel.Dispose ();
				conceptsLabel = null;
			}

			if (sampleLogLabel != null) {
				sampleLogLabel.Dispose ();
				sampleLogLabel = null;
			}

			if (headerReLabel != null) {
				headerReLabel.Dispose ();
				headerReLabel = null;
			}

			if (transformLabel != null) {
				transformLabel.Dispose ();
				transformLabel = null;
			}

			if (testLabel != null) {
				testLabel.Dispose ();
				testLabel = null;
			}
		}
	}
}
