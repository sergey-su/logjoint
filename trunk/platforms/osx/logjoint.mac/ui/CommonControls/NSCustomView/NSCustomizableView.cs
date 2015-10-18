
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	[MonoMac.Foundation.Register("NSCustomizableView")]
	public partial class NSCustomizableView : MonoMac.AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public NSCustomizableView(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public NSCustomizableView(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public override bool IsFlipped
		{
			get
			{
				return true;
			}
		}

		public override void DrawRect(System.Drawing.RectangleF dirtyRect)
		{
			if (BackgroundColor != null)
			{
				BackgroundColor.SetFill();
				NSBezierPath.FillRect(dirtyRect);
			}
			else
			{
				base.DrawRect(dirtyRect);
			}
			if (OnPaint != null)
			{
				OnPaint(dirtyRect);
			}
		}

		public override void ScrollWheel(NSEvent theEvent)
		{
			if (OnScrollWheel != null)
				OnScrollWheel(theEvent);
			else
				base.ScrollWheel(theEvent);
		}

		public override void MagnifyWithEvent(NSEvent theEvent)
		{
			if (OnMagnify != null)
				OnMagnify(theEvent);
			else
				base.MagnifyWithEvent(theEvent);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			if (OnMouseDown != null)
				OnMouseDown(theEvent);
			else
				base.MouseDown(theEvent);
		}

		public override void MouseUp(NSEvent theEvent)
		{
			if (OnMouseUp != null)
				OnMouseUp(theEvent);
			else
				base.MouseUp(theEvent);
		}

		public override void MouseMoved(NSEvent theEvent)
		{
			if (OnMouseMove != null)
				OnMouseMove(theEvent);
			else
				base.MouseMoved(theEvent);
		}

		public override void MouseDragged(NSEvent theEvent)
		{
			if (OnMouseDragged != null)
				OnMouseDragged(theEvent);
			else
				base.MouseDragged(theEvent);
		}

		public override bool AcceptsFirstResponder()
		{
			return CanBeFirstResponder;
		}

		public NSColor BackgroundColor;
		public bool CanBeFirstResponder;
		public Action<System.Drawing.RectangleF> OnPaint;
		public Action<NSEvent> OnScrollWheel;
		public Action<NSEvent> OnMagnify;
		public Action<NSEvent> OnMouseDown;
		public Action<NSEvent> OnMouseUp;
		public Action<NSEvent> OnMouseMove;
		public Action<NSEvent> OnMouseDragged;
	}
}

