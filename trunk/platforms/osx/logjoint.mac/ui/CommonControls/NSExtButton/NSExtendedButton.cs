using System;
using AppKit;
using Foundation;

namespace LogJoint.UI
{
	[Register("NSExtendedButton")]
	public class NSExtendedButton : AppKit.NSButton
	{
		public NSExtendedButton (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public NSExtendedButton (NSCoder coder) : base (coder)
		{
		}

		public NSExtendedButton(): base()
		{
		}

		public event EventHandler OnDblClicked;

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);
			if (theEvent.ClickCount >= 2)
				OnDblClicked?.Invoke(this, EventArgs.Empty);
		}
	}
}