// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	[Register ("TimeSeriesWindowController")]
	partial class TimeSeriesWindowController
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel configureLinkLabel { get; set; }

		[Outlet]
		LogJoint.UI.Postprocessing.TimeSeriesVisualizer.NSDynamicCollectionView legendItemsCollectionView { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView plotsView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel resetAxisLinkLabel { get; set; }

		[Outlet]
		AppKit.NSButton warningsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView xAxisView { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView yAxesView { get; set; }

		[Outlet]
		AppKit.NSLayoutConstraint yAxisWidthConstraint { get; set; }

		[Action ("warningButtonClicked:")]
		partial void warningButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (configureLinkLabel != null) {
				configureLinkLabel.Dispose ();
				configureLinkLabel = null;
			}

			if (plotsView != null) {
				plotsView.Dispose ();
				plotsView = null;
			}

			if (resetAxisLinkLabel != null) {
				resetAxisLinkLabel.Dispose ();
				resetAxisLinkLabel = null;
			}

			if (xAxisView != null) {
				xAxisView.Dispose ();
				xAxisView = null;
			}

			if (yAxesView != null) {
				yAxesView.Dispose ();
				yAxesView = null;
			}

			if (yAxisWidthConstraint != null) {
				yAxisWidthConstraint.Dispose ();
				yAxisWidthConstraint = null;
			}

			if (legendItemsCollectionView != null) {
				legendItemsCollectionView.Dispose ();
				legendItemsCollectionView = null;
			}

			if (warningsButton != null) {
				warningsButton.Dispose ();
				warningsButton = null;
			}
		}
	}

	[Register ("TimeSeriesWindow")]
	partial class TimeSeriesWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
