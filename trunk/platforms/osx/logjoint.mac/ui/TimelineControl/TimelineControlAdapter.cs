using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.Timeline;
using System.Drawing;
using LJD = LogJoint.Drawing;
using LogJoint.UI.Timeline;

namespace LogJoint.UI
{
	public partial class TimelineControlAdapter : NSViewController, IView
	{
		IViewEvents viewEvents;
		ControlDrawing drawing;
		Lazy<int> dateAreaHeight;

		#region Constructors

		// Called when created from unmanaged code
		public TimelineControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public TimelineControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public TimelineControlAdapter()
			: base("TimelineControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
			drawing = new ControlDrawing(
				new GraphicsResources(
					NSFont.SystemFontOfSize(NSFont.SystemFontSize).FontName,
					NSFont.SystemFontSize,
					NSFont.SystemFontSize * 0.6f,
					LJD.Extensions.ToColor(NSColor.ControlBackground),
					new LJD.Image(NSImage.ImageNamed("TimelineBookmark.png"))
				)
			);
			dateAreaHeight = new Lazy<int>(() =>
			{
				using (var g = new LJD.Graphics())
					return drawing.MeasureDatesAreaHeight(g);
			});
		}

		#endregion

		//strongly typed view accessor
		public new TimelineControl View
		{
			get
			{
				return (TimelineControl)base.View;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			timelineView.OnPaint = TimelinePaint;
			timelineView.OnMouseDown = TimelineMouseDown;
			timelineView.OnMouseMove = TimelineMouseMove;
			timelineView.OnMouseLeave = TimelineMouseLeave;
			timelineView.OnScrollWheel = TimelineMouseWheel;
			timelineView.OnMagnify = TimelineMagnify;

			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, ns => TimelineResized(), timelineView);
			
		}

		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.Invalidate()
		{
			timelineView.NeedsDisplay = true;
		}

		void IView.RepaintNow()
		{
			timelineView.NeedsDisplay = true;
		}

		void IView.UpdateDragViewPositionDuringAnimation(int y, bool topView)
		{
			// todo
		}

		PresentationMetrics IView.GetPresentationMetrics()
		{
			return GetMetrics().ToPresentationMetrics();
		}

		HitTestResult IView.HitTest(int x, int y)
		{
			return GetMetrics().HitTest(new Point(x, y));
		}

		void IView.TryBeginDrag(int x, int y)
		{
			// dates dragging is not supported on mac
		}

		void IView.InterruptDrag()
		{
			// dates dragging is not supported on mac
		}

		void IView.ResetToolTipPoint(int x, int y)
		{
			// todo
		}

		void IView.SetHScoll(bool isVisible, int innerViewWidth)
		{
			// 
		}

		void TimelinePaint(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics())
			{
				drawing.FillBackground(g, dirtyRect);

				Metrics m = GetMetrics();

				var drawInfo = viewEvents.OnDraw(m.ToPresentationMetrics());
				if (drawInfo == null)
					return;

				drawing.DrawSources(g, drawInfo);
				drawing.DrawRulers(g, m, drawInfo);
				drawing.DrawDragAreas(g, m, drawInfo);
				drawing.DrawBookmarks(g, m, drawInfo);
				drawing.DrawCurrentViewTime(g, m, drawInfo);
				drawing.DrawHotTrackRange(g, m, drawInfo);
				drawing.DrawHotTrackDate(g, m, drawInfo);
			}
		}

		void TimelineMouseDown(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			if (e.ClickCount >= 2)
				viewEvents.OnMouseDblClick((int)pt.X, (int)pt.Y);
			else
				viewEvents.OnLeftMouseDown((int)pt.X, (int)pt.Y);
		}

		void TimelineMouseWheel(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			bool zoom = (e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0;
			if (zoom)
				viewEvents.OnMouseWheel((int)pt.X, (int)pt.Y, e.DeltaY / 20d, true);
			else
				viewEvents.OnMouseWheel((int)pt.X, (int)pt.Y, -e.DeltaY / 20d, false);
		}

		void TimelineMagnify(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			viewEvents.OnMagnify((int)pt.X, (int)pt.Y, -e.Magnification);
		}

		void TimelineMouseMove(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			viewEvents.OnMouseMove((int)pt.X, (int)pt.Y);
		}

		void TimelineMouseLeave(NSEvent e)
		{
			viewEvents.OnMouseLeave();
		}

		void TimelineResized()
		{
			if (viewEvents != null)
				viewEvents.OnTimelineClientSizeChanged();
		}

		Metrics GetMetrics()
		{
			return new Metrics(
				LJD.Extensions.ToRectangle(timelineView.Frame),
				dateAreaHeight.Value,
				0,
				40
			);
		}
	}
}

