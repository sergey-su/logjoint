using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class OptionsWindow : NSWindow
	{
		public OptionsWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public OptionsWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
