using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class EditSampleLogDialog : NSWindow
	{
		public EditSampleLogDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public EditSampleLogDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
