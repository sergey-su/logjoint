﻿using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using LJD = LogJoint.Drawing;
using ObjCRuntime;
using LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesWindowController : 
		AppKit.NSWindowController,
		IView,
		Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm
	{
		IViewEvents eventsHandler;
		Drawing.Resources resources;
		CollectionViewDataSource legendDataSource;
		ToastNotificationsViewAdapter toastNotifications;

		#region Constructors

		// Called when created from unmanaged code
		public TimeSeriesWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimeSeriesWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public TimeSeriesWindowController () : base ("TimeSeriesWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
			var sysFont = NSFont.SystemFontOfSize (NSFont.SystemFontSize);
			resources = new Drawing.Resources(sysFont.FontName, 
			                                  (float) NSFont.SystemFontSize, 
			                                  bookmarkIcon: new LJD.Image(NSImage.ImageNamed("TimelineBookmark.png")));
			legendDataSource = new CollectionViewDataSource() { Resources = resources };
			toastNotifications = new ToastNotificationsViewAdapter ();
		}

		#endregion

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			this.legendDataSource.Events = eventsHandler;
		}

		void IView.Invalidate ()
		{
			if (plotsView == null) // not loaded from nib yet
				return;
			plotsView.NeedsDisplay = true;
			xAxisView.NeedsDisplay = true;
			yAxesView.NeedsDisplay = true;
		}

		IConfigDialogView IView.CreateConfigDialogView (IConfigDialogEventsHandler evts)
		{
			return new TimeSeriesConfigWindowController(evts, resources);
		}

		Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView
		{
			get { return toastNotifications; }
		}

		void IView.SetNotificationsIconVisibility(bool value)
		{
			warningsButton.Hidden = !value;
		}

		void IView.UpdateYAxesSize ()
		{
			UpdateYAxesSize ();
		}

		void IView.UpdateLegend (IEnumerable<LegendItemInfo> items)
		{
			// todo: update incrementally
			legendDataSource.Data.Clear();
			foreach (var i in items)
				legendDataSource.Data.Add(i);
			if (legendItemsCollectionView != null)
				legendItemsCollectionView.ReloadData();
		}

		PlotsViewMetrics IView.PlotsViewMetrics
		{
			get { return GetPlotsViewMetrics(); }
		}

		void Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm.Show ()
		{
			InvokeOnMainThread(() => eventsHandler.OnShown());
			Window.MakeKeyAndOrderFront (null);
		}

		public new TimeSeriesWindow Window 
		{
			get { return (TimeSeriesWindow)base.Window; }
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Window.owner = this;

			configureLinkLabel.StringValue = "configure view...";
			configureLinkLabel.LinkClicked = (sender, e) => 
			{
				eventsHandler.OnConfigViewClicked ();
			};

			resetAxisLinkLabel.StringValue = "reset axes";
			resetAxisLinkLabel.LinkClicked = (sender, e) => 
			{
				eventsHandler.OnResetAxesClicked ();
			};

			plotsView.OnPaint = (RectangleF ditryRect) => 
			{
				using (var g = new Graphics ())
				{
					g.FillRectangle(Brushes.TextBackground, ditryRect);
					if (eventsHandler != null)
						Drawing.DrawPlotsArea (g, resources,
							eventsHandler.OnDrawPlotsArea (), GetPlotsViewMetrics ());
				}
			};

			xAxisView.OnPaint = (RectangleF ditryRect) => 
			{
				using (var g = new Graphics ())
				{
					g.FillRectangle(Brushes.TextBackground, ditryRect);
					if (eventsHandler != null)
						Drawing.DrawXAxis (g, resources,
							eventsHandler.OnDrawPlotsArea (), (float)xAxisView.Bounds.Height);
				}
			};

			yAxesView.OnPaint = (RectangleF ditryRect) => 
			{
				using (var g = new Graphics ())
				{
					g.FillRectangle(Brushes.TextBackground, ditryRect);
					if (eventsHandler != null)
						Drawing.DrawYAxes (g, resources,
							eventsHandler.OnDrawPlotsArea (), (float)yAxesView.Bounds.Width, GetPlotsViewMetrics ());
				}
			};

			plotsView.OnMouseMove = e =>
			{
				var pt = plotsView.ConvertPointFromView(e.LocationInWindow, null).ToPointF();
				var toolTip = eventsHandler.OnTooltip (pt);
				if (toolTip != null)
					plotsView.ToolTip = toolTip;
				else
					plotsView.RemoveAllToolTips ();				
			};

			Action<NSCustomizableView> initView = view => 
			{
				view.OnMouseDown = (NSEvent evt) => { HandleMouseDown (view, evt); };
				view.OnMouseUp = (NSEvent evt) => { HandleMouseUp (view, evt); };
				view.OnMouseDragged = (NSEvent evt) => { HandleMouseMove (view, evt); };
				view.OnMagnify = (NSEvent evt) => { HandleMagnify (view, evt); };
				view.OnScrollWheel = (NSEvent evt) => { HandleScrollWheel (view, evt); };
			};

			initView (plotsView);
			initView (yAxesView);
			initView (xAxisView);

			UpdateYAxesSize ();

			InitLegendItemsCollectionView ();

			PlaceToastNotificationsView(toastNotifications.View, plotsView);
		}

		private void InitLegendItemsCollectionView ()
		{
			legendItemsCollectionView.DataSource = legendDataSource;
			legendItemsCollectionView.RegisterClassForItem (typeof (LegendItemController), "LegendItemCell");
			legendItemsCollectionView.CollectionViewLayout = new NSCollectionViewFlowLayout () 
			{
				SectionInset = new NSEdgeInsets (2, 2, 2, 2),
				MinimumInteritemSpacing = 4,
				MinimumLineSpacing = 1
			};
			legendItemsCollectionView.WantsLayer = true;
			legendItemsCollectionView.Delegate = new CollectionViewDelegate(legendDataSource, resources);
			legendItemsCollectionView.ReloadData();
		}

		static void PlaceToastNotificationsView(NSView toastNotificationsView, NSView parent)
		{
			parent.AddSubview (toastNotificationsView);
			toastNotificationsView.TranslatesAutoresizingMaskIntoConstraints = false;
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Trailing, 1f, 2f));
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Top, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Top, 1f, 2f));
		}

		partial void warningButtonClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnActiveNotificationButtonClicked();
		}

		void HandleMouseDown(NSCustomizableView sender, NSEvent evt)
		{
			var pt = sender.ConvertPointFromView(evt.LocationInWindow, null).ToPointF();
			eventsHandler.OnMouseDown(GetViewPart(sender, pt.ToPoint()), pt, (int) evt.ClickCount);
		}

		void HandleMouseUp(NSCustomizableView sender, NSEvent evt)
		{
			var pt = sender.ConvertPointFromView(evt.LocationInWindow, null).ToPointF();
			eventsHandler.OnMouseUp(GetViewPart(sender, pt.ToPoint()), pt);
		}

		void HandleMouseMove(NSCustomizableView sender, NSEvent evt)
		{
			var pt = sender.ConvertPointFromView(evt.LocationInWindow, null).ToPointF();
			eventsHandler.OnMouseMove(GetViewPart(sender, pt.ToPoint()), pt);
		}

		void HandleScrollWheel(NSCustomizableView sender, NSEvent evt)
		{
			var pt = sender.ConvertPointFromView(evt.LocationInWindow, null).ToPointF();
			eventsHandler.OnMouseWheel(GetViewPart(sender, pt.ToPoint()), 
				new SizeF(-(float)evt.ScrollingDeltaX, (float)evt.ScrollingDeltaY));
		}

		void HandleMagnify(NSCustomizableView sender, NSEvent evt)
		{
			var pt = sender.ConvertPointFromView(evt.LocationInWindow, null).ToPointF();
			eventsHandler.OnMouseZoom(GetViewPart(sender, pt.ToPoint()), pt, 1f - (float) evt.Magnification);
		}

		ViewPart GetViewPart(object sender, Point pt)
		{
			if (sender == plotsView)
				return new ViewPart()
			{
				Part = ViewPart.PartId.Plots
			};
			else if (sender == xAxisView)
				return new ViewPart()
			{
				Part = ViewPart.PartId.XAxis,
				AxisId = eventsHandler.OnDrawPlotsArea().XAxis.Id,
			};
			else if (sender == yAxesView)
				using (var g = new LJD.Graphics())
					return new ViewPart()
				{
					Part = ViewPart.PartId.YAxis,
					AxisId = Drawing.GetYAxisId(g, resources, eventsHandler.OnDrawPlotsArea(), 
						pt.X, (float) yAxesView.Bounds.Width)
				};
			return new ViewPart();
		}

		void UpdateYAxesSize ()
		{
			if (yAxisWidthConstraint != null)
				using (var g = new Graphics ())
					yAxisWidthConstraint.Constant = Drawing.GetYAxesMetrics (g, resources, eventsHandler.OnDrawPlotsArea ())
						.Select (x => x.Width).Sum ();
		}

		PlotsViewMetrics GetPlotsViewMetrics()
		{
			return new PlotsViewMetrics()
			{
				Size = plotsView != null ? plotsView.Bounds.Size.ToSizeF() : new SizeF(1, 1)
			};
		}

		internal void OnCancelOp()
		{
			Window.Close();
		}

		internal void OnKeyEvent(KeyCode key)
		{
			eventsHandler.OnKeyDown(key);
		}
	}
}

