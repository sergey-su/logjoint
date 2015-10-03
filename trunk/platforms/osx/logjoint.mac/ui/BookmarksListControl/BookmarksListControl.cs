
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class BookmarksListControl : MonoMac.AppKit.NSView
	{
		BookmarksListControlAdapter owner;

		#region Constructors

		// Called when created from unmanaged code
		public BookmarksListControl(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public BookmarksListControl(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public void Initialize(BookmarksListControlAdapter owner)
		{
			this.owner = owner;
		}

		public override void KeyDown(NSEvent theEvent)
		{
			this.InterpretKeyEvents(new [] { theEvent });
		}

		[Export ("deleteBackward:")]
		void OnDeleteBackward (NSObject theEvent)
		{
			owner.ViewEvents.OnDeleteButtonPressed();
		}

		[Export ("forwardBackward:")]
		void OnForwardBackward (NSObject theEvent)
		{
			owner.ViewEvents.OnDeleteButtonPressed();
		}

		[Export ("insertNewline:")]
		void OnInsertNewline (NSObject theEvent)
		{
			owner.ViewEvents.OnEnterKeyPressed();
		}

		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return true;
		}
			
		[Export ("copy:")]
		void OnCopy (NSObject theEvent)
		{
			// todo: handle ctrl+c, ctrl+ins too
			owner.ViewEvents.OnCopyShortcutPressed();
		}
	}
}

