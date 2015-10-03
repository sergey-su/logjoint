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
	[Register ("SearchResultsControlAdapter")]
	partial class SearchResultsControlAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSView logViewerPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator searchProgress { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField searchResultLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField searchStatusLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (searchProgress != null) {
				searchProgress.Dispose ();
				searchProgress = null;
			}

			if (logViewerPlaceholder != null) {
				logViewerPlaceholder.Dispose ();
				logViewerPlaceholder = null;
			}

			if (searchResultLabel != null) {
				searchResultLabel.Dispose ();
				searchResultLabel = null;
			}

			if (searchStatusLabel != null) {
				searchStatusLabel.Dispose ();
				searchStatusLabel = null;
			}
		}
	}

	[Register ("SearchResultsControl")]
	partial class SearchResultsControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
