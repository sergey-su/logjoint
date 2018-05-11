using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class SearchesManagerDialog : NSWindow
	{
		public SearchesManagerDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchesManagerDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSEvent theEvent)
		{
			NSApplication.SharedApplication.StopModal();
			this.Close();
		}
	}
}
