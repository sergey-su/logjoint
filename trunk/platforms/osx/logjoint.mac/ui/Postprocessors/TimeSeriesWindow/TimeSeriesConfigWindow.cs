using System;

using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesConfigWindow : NSWindow
	{
		internal TimeSeriesConfigWindowController owner;
		
		public TimeSeriesConfigWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public TimeSeriesConfigWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			owner.OnCancelOp ();
		}
	}
}
