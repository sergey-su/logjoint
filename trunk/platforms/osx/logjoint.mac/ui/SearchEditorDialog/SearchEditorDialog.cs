using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class SearchEditorDialog : NSWindow
	{
		public SearchEditorDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchEditorDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
