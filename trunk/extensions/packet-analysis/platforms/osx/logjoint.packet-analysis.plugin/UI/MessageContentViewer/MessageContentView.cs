using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.PacketAnalysis.UI
{
	public partial class MessageContentView : AppKit.NSView
	{
		public Action onCopy;

		#region Constructors

		// Called when created from unmanaged code
		public MessageContentView(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public MessageContentView(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		// Shared initialization code
		void Initialize()
		{
		}

		[Export("copy:")]
		public void OnCopy(NSObject _) => onCopy?.Invoke();

		#endregion
	}
}
