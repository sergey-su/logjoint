
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.UI
{
	public partial class LogViewerControl : MonoMac.AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public LogViewerControl(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public LogViewerControl(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public void Init(LogViewerControlAdapter owner)
		{
			this.owner = owner;
		}

		public override void DrawRect(RectangleF dirtyRect)
		{
			owner.OnPaint(dirtyRect);
		}

		public override bool IsFlipped
		{
			get
			{
				return true;
			}
		}

		public override bool AcceptsFirstResponder()
		{
			return true;
		}
			

		#if kljkj
		public override void KeyDown(NSEvent theEvent)
		{
			bool ctrl = (theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0;
			bool alt = (theEvent.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0;
			bool shift = (theEvent.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0;
			switch (theEvent.KeyCode)
			{
				case 124:
					owner.viewEvents.OnKeyPressed(Key.Right, ctrl, alt, shift);
					break;
				case 123:
					owner.viewEvents.OnKeyPressed(Key.Left, ctrl, alt, shift);
					break;
				case 125:
					owner.viewEvents.OnKeyPressed(Key.Down, ctrl, alt, shift);
					break;
				case 126:
					owner.viewEvents.OnKeyPressed(Key.Up, ctrl, alt, shift);
					break;
				case 11:
					owner.viewEvents.OnKeyPressed(Key.B, ctrl, alt, shift);
					break;
				case 121:
					owner.viewEvents.OnKeyPressed(Key.PageDown, ctrl, alt, shift);
					break;
				case 116:
					owner.viewEvents.OnKeyPressed(Key.PageUp, ctrl, alt, shift);
					break;
				case 115:
					owner.viewEvents.OnKeyPressed(Key.Home, ctrl, alt, shift);
					break;
				case 119:
					owner.viewEvents.OnKeyPressed(Key.End, ctrl, alt, shift);
					break;
			}
		}
		#endif


		[Export ("moveUp:")]
		void OnMoveUp (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Up, m.ctrl, m.alt, m.shift);
		}

		[Export ("moveDown:")]
		void OnMoveDown (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Down, m.ctrl, m.alt, m.shift);
		}

		[Export ("moveRight:")]
		void OnMoveRight (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Right, m.ctrl, m.alt, m.shift);
		}

		[Export ("moveLeft:")]
		void OnMoveLeft (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Left, m.ctrl, m.alt, m.shift);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			owner.OnMouseDown(theEvent);
			base.MouseDown(theEvent);
		}

		struct Modifiers
		{
			public bool ctrl, alt, shift;
			public Modifiers()
			{
				var theEvent = NSApplication.SharedApplication.CurrentEvent;
				if (theEvent != null)
				{
					ctrl = (theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0;
					alt = (theEvent.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0;
					shift = (theEvent.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0;
				}
				else
				{
					ctrl = false;
					alt = false;
					shift = false;
				}
			}
		}

		LogViewerControlAdapter owner;
	}
}

