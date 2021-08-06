using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Timeline;
using LogJoint.Drawing;
using LogJoint.UI.Timeline;

namespace LogJoint.UI
{
	public partial class TimelineControlAdapter : NSViewController, IView
	{
		IViewModel viewModel;
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

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			drawing = new ControlDrawing (
				new GraphicsResources (
					viewModel,
					NSFont.SystemFontOfSize (NSFont.SystemFontSize).FontName,
					(float)NSFont.SystemFontSize,
					(float)NSFont.SystemFontSize * 0.6f,
					new Image (NSImage.ImageNamed ("TimelineBookmark.png"))
				),
				viewModel
			);
			dateAreaHeight = new Lazy<int> (() => {
				using (var g = new Graphics ())
					return drawing.MeasureDatesAreaHeight (g);
			});

			var updater = Updaters.Create (() => viewModel.OnDraw (), _ => {
				timelineView.NeedsDisplay = true;
			});
			viewModel.ChangeNotification.OnChange += (s, e) => updater ();
		}

		void IView.UpdateDragViewPositionDuringAnimation(int y, bool topView)
		{
			// todo
		}

		PresentationMetrics IView.GetPresentationMetrics()
		{
			return GetMetrics().ToPresentationMetrics();
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

		void TimelinePaint(RectangleF dirtyRect)
		{
			if (drawing == null)
				return;
			using (var g = new Graphics())
			{
				drawing.FillBackground(g, dirtyRect);

				Metrics m = GetMetrics();

				var drawInfo = viewModel.OnDraw();
				if (drawInfo == null)
					return;

				drawing.DrawSources(g, drawInfo);
				drawing.DrawContainerControls(g, drawInfo);
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
			var area = GetMetrics ().HitTest (pt.ToPoint());
			if (e.ClickCount >= 2)
				viewModel.OnMouseDblClick((int)pt.X, (int)pt.Y, area);
			else
				viewModel.OnLeftMouseDown((int)pt.X, (int)pt.Y, area);
		}

		void TimelineMouseWheel(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			bool zoom = (e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0;
			var area = GetMetrics ().HitTest (pt.ToPoint ());
			if (zoom)
				viewModel.OnMouseWheel((int)pt.X, (int)pt.Y, e.DeltaY / 20d, true, area);
			else
				viewModel.OnMouseWheel((int)pt.X, (int)pt.Y, -e.DeltaY / 20d, false, area);
		}

		void TimelineMagnify(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			viewModel.OnMagnify((int)pt.X, (int)pt.Y, -e.Magnification);
		}

		void TimelineMouseMove(NSEvent e)
		{
			var pt = timelineView.GetEventLocation(e);
			viewModel.OnMouseMove((int)pt.X, (int)pt.Y,
				GetMetrics ().HitTest (pt.ToPoint ()));
		}

		void TimelineMouseLeave(NSEvent e)
		{
			viewModel.OnMouseLeave();
		}

		void TimelineResized()
		{
			if (viewModel != null)
				viewModel.ChangeNotification.Post ();
		}

		Metrics GetMetrics()
		{
			return new Metrics(
				timelineView.Frame.ToRectangle(),
				dateAreaHeight.Value,
				0,
				40
			);
		}
	}
}

