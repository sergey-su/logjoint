using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class FiltersList : AppKit.NSView
	{
		internal FiltersListController owner;

		#region Constructors

		// Called when created from unmanaged code
		public FiltersList (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FiltersList (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		public override void KeyDown(NSEvent theEvent)
		{
			this.InterpretKeyEvents(new [] { theEvent });
		}

		[Export ("deleteBackward:")]
		void OnDeleteBackward (NSObject theEvent)
		{
			owner.eventsHandler.OnDeletePressed();
		}

		[Export ("forwardBackward:")]
		void OnForwardBackward (NSObject theEvent)
		{
			owner.eventsHandler.OnDeletePressed();
		}


		[Export ("insertNewline:")]
		void OnInsertNewline (NSObject theEvent)
		{
			owner.eventsHandler.OnEnterPressed();
		}
	}
}
