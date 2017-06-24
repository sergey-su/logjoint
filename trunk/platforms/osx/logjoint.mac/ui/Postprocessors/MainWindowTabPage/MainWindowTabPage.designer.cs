// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.MainWindowTabPage
{
	[Register ("MainWindowTabPageAdapter")]
	partial class MainWindowTabPageAdapter
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel allPostprocessorsAction { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator allPostprocessorsProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel cloudDownloaderAction1 { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel cloudDownloaderAction2 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator cloudDownloaderProgressIndicator2 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator cloudLogsDownloaderProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel correlationAction1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator correlationProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel openGenericLogAction { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel sequenceAction1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator sequenceProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel stateInspectorAction1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator stateInspectorProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel timelineAction1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator timelineProgressIndicator { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel timeSeriesAction1 { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator timeSeriesProgressIndicator { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (allPostprocessorsAction != null) {
				allPostprocessorsAction.Dispose ();
				allPostprocessorsAction = null;
			}

			if (timeSeriesAction1 != null) {
				timeSeriesAction1.Dispose ();
				timeSeriesAction1 = null;
			}

			if (allPostprocessorsProgressIndicator != null) {
				allPostprocessorsProgressIndicator.Dispose ();
				allPostprocessorsProgressIndicator = null;
			}

			if (cloudDownloaderAction1 != null) {
				cloudDownloaderAction1.Dispose ();
				cloudDownloaderAction1 = null;
			}

			if (cloudDownloaderAction2 != null) {
				cloudDownloaderAction2.Dispose ();
				cloudDownloaderAction2 = null;
			}

			if (cloudDownloaderProgressIndicator2 != null) {
				cloudDownloaderProgressIndicator2.Dispose ();
				cloudDownloaderProgressIndicator2 = null;
			}

			if (cloudLogsDownloaderProgressIndicator != null) {
				cloudLogsDownloaderProgressIndicator.Dispose ();
				cloudLogsDownloaderProgressIndicator = null;
			}

			if (correlationAction1 != null) {
				correlationAction1.Dispose ();
				correlationAction1 = null;
			}

			if (correlationProgressIndicator != null) {
				correlationProgressIndicator.Dispose ();
				correlationProgressIndicator = null;
			}

			if (openGenericLogAction != null) {
				openGenericLogAction.Dispose ();
				openGenericLogAction = null;
			}

			if (sequenceAction1 != null) {
				sequenceAction1.Dispose ();
				sequenceAction1 = null;
			}

			if (sequenceProgressIndicator != null) {
				sequenceProgressIndicator.Dispose ();
				sequenceProgressIndicator = null;
			}

			if (stateInspectorAction1 != null) {
				stateInspectorAction1.Dispose ();
				stateInspectorAction1 = null;
			}

			if (stateInspectorProgressIndicator != null) {
				stateInspectorProgressIndicator.Dispose ();
				stateInspectorProgressIndicator = null;
			}

			if (timelineAction1 != null) {
				timelineAction1.Dispose ();
				timelineAction1 = null;
			}

			if (timelineProgressIndicator != null) {
				timelineProgressIndicator.Dispose ();
				timelineProgressIndicator = null;
			}

			if (timeSeriesProgressIndicator != null) {
				timeSeriesProgressIndicator.Dispose ();
				timeSeriesProgressIndicator = null;
			}
		}
	}

	[Register ("MainWindowTabPage")]
	partial class MainWindowTabPage
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
