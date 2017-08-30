using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class TestFormatDialog : NSWindow
	{
		public TestFormatDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public TestFormatDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
