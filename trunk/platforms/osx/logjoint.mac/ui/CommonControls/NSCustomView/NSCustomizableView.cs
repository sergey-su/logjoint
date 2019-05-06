
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	[Foundation.Register("NSCustomizableView")]
	public partial class NSCustomizableView : AppKit.NSView
	{
		Action<NSEvent> mouseMove, mouseLeave;
		NSTrackingArea trackingArea;

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

		public NSCustomizableView()
		{
			Initialize();
		}

		// Shared initialization code
		void Initialize()
		{
		}

		#endregion


		public NSColor BackgroundColor;
		public bool CanBeFirstResponder;
		public Action<RectangleF> OnPaint;
		public Action<NSEvent> OnScrollWheel;
		public Action<NSEvent> OnMagnify;
		public Action<NSEvent> OnMouseDown;
		public Action<NSEvent> OnMouseUp;
		public Action<NSEvent> OnMouseDragged;
		public Action<NSEvent> OnMouseMove { get { return mouseMove; } set { mouseMove = value; UpdateTrackingAreas(); } }
		public Action<NSEvent> OnMouseLeave { get { return mouseLeave; } set { mouseLeave = value; UpdateTrackingAreas(); } }
		public Action OnResignFirstResponder;


		public override bool IsFlipped
		{
			get
			{
				return true;
			}
		}

		public override void DrawRect(CGRect dirtyRect)
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
				OnPaint(dirtyRect.ToRectangleF ());
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

		public override void MouseExited(NSEvent theEvent)
		{
			if (OnMouseLeave != null)
				OnMouseLeave(theEvent);
			else
				base.MouseExited(theEvent);
		}

		public override bool AcceptsFirstResponder()
		{
			return CanBeFirstResponder;
		}

		public override bool ResignFirstResponder()
		{
			if (OnResignFirstResponder != null)
				OnResignFirstResponder();
			return base.ResignFirstResponder();
		}

		public override void UpdateTrackingAreas()
		{
			if (trackingArea != null) // remove existing area if previously set
			{
				RemoveTrackingArea(trackingArea);
				trackingArea = null;
			}

			var opts = (NSTrackingAreaOptions)0;
			if (mouseMove != null)
				opts |= NSTrackingAreaOptions.MouseMoved;
			if (mouseLeave != null)
				opts |= NSTrackingAreaOptions.MouseEnteredAndExited;

			if (opts != (NSTrackingAreaOptions)0) // add new area if required
			{
				opts |= NSTrackingAreaOptions.ActiveAlways | NSTrackingAreaOptions.InVisibleRect;
				trackingArea = new NSTrackingArea(this.Bounds, opts, this, null);
				AddTrackingArea(trackingArea);
			}

			base.UpdateTrackingAreas();
		}
	}
}

