using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class FilterDialog : NSPanel
	{
		public FilterDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public FilterDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
