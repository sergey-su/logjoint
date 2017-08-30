using System;

using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class FormatsWizardDialog : NSWindow
	{
		public FormatsWizardDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public FormatsWizardDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
