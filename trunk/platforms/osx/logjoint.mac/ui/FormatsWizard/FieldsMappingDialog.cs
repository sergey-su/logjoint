using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class FieldsMappingDialog : NSWindow
	{
		public FieldsMappingDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public FieldsMappingDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
