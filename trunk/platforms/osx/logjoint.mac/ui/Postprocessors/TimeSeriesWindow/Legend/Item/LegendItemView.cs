using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class LegendItemView : NSView
	{
		
		#region Constructors
		// Called when created from unmanaged code
		public LegendItemView(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public LegendItemView(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		// Shared initialization code
		void Initialize()
		{
		}
		#endregion
	}
}
