using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class XsltEditorDialog : NSWindow
	{
		public XsltEditorDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public XsltEditorDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
