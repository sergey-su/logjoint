
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class MessagePropertiesDialogAdapter : AppKit.NSWindowController
	{
		#region Constructors

		// Called when created from unmanaged code
		public MessagePropertiesDialogAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public MessagePropertiesDialogAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public MessagePropertiesDialogAdapter()
			: base("MessagePropertiesDialog")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed window accessor
		public new MessagePropertiesDialog Window
		{
			get
			{
				return (MessagePropertiesDialog)base.Window;
			}
		}
	}
}

