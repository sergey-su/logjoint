using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesWindow : AppKit.NSWindow
	{
		#region Constructors

		// Called when created from unmanaged code
		public TimeSeriesWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimeSeriesWindow (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion	
	}
}

