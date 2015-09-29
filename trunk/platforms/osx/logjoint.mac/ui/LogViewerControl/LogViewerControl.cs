
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
			}
		}

		public override void MouseDown(NSEvent theEvent)
		{
			owner.OnMouseDown(theEvent);
			base.MouseDown(theEvent);
		}

		LogViewerControlAdapter owner;
	}
}

