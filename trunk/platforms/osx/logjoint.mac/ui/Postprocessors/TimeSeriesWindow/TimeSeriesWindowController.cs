using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using System.Drawing;
using LJD = LogJoint.Drawing;
using ObjCRuntime;
using LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesWindowController : 
		AppKit.NSWindowController,
		IView,
		Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm
	{
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public TimeSeriesWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimeSeriesWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public TimeSeriesWindowController () : base ("TimeSeriesWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Invalidate ()
		{
			// todo
		}

		IConfigDialogView IView.CreateConfigDialogView (IConfigDialogEventsHandler evts)
		{
			// todo
			return null;
		}

		void IView.UpdateYAxesSize ()
		{
			// todo
		}

		void IView.UpdateLegend (IEnumerable<LegendItemInfo> items)
		{
			// todo
		}

		PlotsViewMetrics IView.PlotsViewMetrics
		{
			get { return new PlotsViewMetrics(); } // todo
		}

		void Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm.Show ()
		{
			Window.MakeKeyAndOrderFront (null);
		}

		public new TimeSeriesWindow Window 
		{
			get { return (TimeSeriesWindow)base.Window; }
		}
	}
}

