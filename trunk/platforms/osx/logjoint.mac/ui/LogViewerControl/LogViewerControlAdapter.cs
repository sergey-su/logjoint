﻿using System;
using System.Collections.Generic;

using System.Drawing;


using Foundation;
using AppKit;
using CoreText;

using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Settings;
using LogJoint.Drawing;
using LJD = LogJoint.Drawing;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using System.Linq;
using System.Threading.Tasks;
using ObjCRuntime;
using LogJoint.UI.LogViewer;

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSResponder, IView
	{
		internal IViewModel viewModel;
		internal bool isFocused;
		string drawDropMessage;
		bool enableCursor = true;
		Profiling.Counters drawingPerfCounters;
		LJD.Graphics.PerformanceCounters graphicsCounters;
		Profiling.Counters.CounterDescriptor controlPaintTimeCounter;
		Profiling.Counters.CounterDescriptor controlPaintWidthCounter;
		Profiling.Counters.CounterDescriptor controlPaintHeightCounter;
		GraphicsResources graphicsResources;
		ViewDrawing viewDrawing;
		readonly Graphics graphicsForMeasurment = new Graphics();
		int viewWidth = 2000;
		SizeF scrollClientAreaSize;

		[Export ("innerView")]
		public LogViewerControl InnerView { get; set;}


		[Export("view")]
		public NSView View { get; set;}

		[Export("scrollView")]
		public NSScrollView ScrollView { get; set;}

		[Export("vertScroller")]
		public NSScroller VertScroller { get; set;}

		[Export("dragDropIconView")]
		public NSCustomizableView DragDropIconView { get; set;}

		public LogViewerControlAdapter()
		{
			NSBundle.LoadNib ("LogViewerControl", this);
			InnerView.Init(this);
			InitScrollView();
			InnerView.Menu = new NSMenu()
			{
				Delegate = new ContextMenuDelegate()
				{
					owner = this
				}
			};
			DragDropIconView.OnPaint = (dirtyRect) => 
			{
				var color = NSColor.Text.ToColor ();
				using (var g = new LJD.Graphics())
				using (var b = new Brush(color))
				{
					float penW = 2;
					var p = new Pen (color, penW, new [] { 5f, 2.5f });
					var r = new RectangleF (new PointF (), DragDropIconView.Frame.Size.ToSizeF ());
					r.Inflate (-penW, -penW);
					g.DrawRoundRectangle (p, r, 25);
					r.Inflate (-5, -5);
					using (var f = new Font (
						NSFont.SystemFontOfSize (NSFont.SystemFontSize).FontName,
						(float)(NSFont.SystemFontSize * 1.2f), FontStyle.Regular))
					{ 
						g.DrawString (
							"Drop logs here\n(files, URLs, archives)",
							f, b, r,
							new StringFormat (StringAlignment.Center, StringAlignment.Center)
						);
					}
				}
			};
		}

		void InitScrollView()
		{
			// without this vert. scrolling with touch gesture often gets stuck
			// if gesture is not absolulety vertical
			ScrollView.HorizontalScrollElasticity = NSScrollElasticity.None;

			ScrollView.PostsFrameChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, async ns =>
			{
				ReadScrollViewSize ();
				viewModel?.ChangeNotification?.Post();
				await Task.Yield(); // w/o this hack inner view is never painted until first resize
				UpdateInnerViewSize();
			}, ScrollView);

			VertScroller.Action = new Selector ("OnVertScrollChanged");
			VertScroller.Target = this;

			ReadScrollViewSize ();
		}

		void ReadScrollViewSize ()
		{
			var f = ScrollView.Frame;
			nfloat testSz = 100;
			var sz = NSScrollView.GetFrameSizeForContent(
				new CoreGraphics.CGSize (testSz, testSz), null, null,
				ScrollView.BorderType, NSControlSize.Regular, ScrollView.ScrollerStyle);
			this.scrollClientAreaSize = new SizeF (
				(float)(f.Width - (sz.Width - testSz)),
				(float)(f.Height - (sz.Height - testSz))
			);
		}

		public bool EnableCursor
		{
			get { return enableCursor; }
			set
			{
				enableCursor = value;
				InnerView.InvalidateCursorRects();
			}
		}

		internal ViewDrawing ViewDrawing => viewDrawing;

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			this.graphicsResources = new GraphicsResources(
				viewModel,
				(fontData) => new LJD.Font(fontData.Name ?? "monaco",
					(float)NSFont.SystemFontSizeForControlSize(NSControlSize.Small)),
				textFormat: null,
				images: (
					error: new LJD.Image(NSImage.ImageNamed("ErrorLogSeverity.png")),
					warn: new LJD.Image(NSImage.ImageNamed("WarnLogSeverity.png")),
					bookmark: new LJD.Image(NSImage.ImageNamed("Bookmark.png")),
					focusedMark: new LJD.Image(NSImage.ImageNamed("FocusedMsg.png"))
				),
				graphicsForMeasurmentFactory: () => graphicsForMeasurment
			);
			this.viewDrawing = new ViewDrawing(viewModel,
				graphicsResources,
				1f,
				() => 0,
				() => viewWidth
			);
			this.drawingPerfCounters = new Profiling.Counters (viewModel.Trace, "drawing");
			this.graphicsCounters = LJD.Graphics.CreateCounters (drawingPerfCounters);
			this.controlPaintTimeCounter = this.drawingPerfCounters.AddCounter ("paint", unit: "ms");
			this.controlPaintWidthCounter = this.drawingPerfCounters.AddCounter ("width", unit: "pixel");
			this.controlPaintHeightCounter = this.drawingPerfCounters.AddCounter ("height", unit: "pixel");
			var viewUpdater = Updaters.Create (
				() => viewModel.ViewLines,
				() => viewModel.FocusedMessageMark,
				(_1, _2) => { this.InnerView.NeedsDisplay = true; }
			);
			var emptyViewMessageUpdater = Updaters.Create (
				() => viewModel.EmptyViewMessage,
				value => {
					drawDropMessage = value;
					DragDropIconView.Hidden = value == null;
				}
			);
			var vScrollUpdater = Updaters.Create(
				() => viewModel.VerticalScrollerPosition,
				value => {
					VertScroller.Enabled = value.HasValue;
					VertScroller.KnobProportion = 0.0001f;
					VertScroller.DoubleValue = value.GetValueOrDefault();
				}
			);
			viewModel.ChangeNotification.CreateSubscription (() => {
				viewUpdater();
				emptyViewMessageUpdater();
				vScrollUpdater();
			});
		}

		void IView.HScrollToSelectedText(int charIndex)
		{
			int pixelThatMustBeVisible = (int)(charIndex * viewDrawing.CharSize.Width);
			pixelThatMustBeVisible += viewDrawing.TimeAreaSize;

			var pos = ScrollView.ContentView.Bounds.Location.ToPointF().ToPoint ();

			int currentVisibleLeft = pos.X;
			int VerticalScrollBarWidth = 50; // todo: how to know it on mac?
			int currentVisibleRight = pos.X + (int)ScrollView.Frame.Width - VerticalScrollBarWidth;
			int extraPixelsAroundSelection = 20;
			if (pixelThatMustBeVisible < pos.X)
			{
				InnerView.ScrollPoint(new CoreGraphics.CGPoint(pixelThatMustBeVisible - extraPixelsAroundSelection, pos.Y));
			}
			if (pixelThatMustBeVisible >= currentVisibleRight)
			{
				InnerView.ScrollPoint(new CoreGraphics.CGPoint(pos.X + (pixelThatMustBeVisible - currentVisibleRight + extraPixelsAroundSelection), pos.Y));
			}

		}

		object IView.GetContextMenuPopupData(int? viewLineIndex)
		{
			// todo
			return null;
		}

		void IView.PopupContextMenu(object contextMenuPopupData)
		{
			// todo
		}

		float IView.DisplayLinesPerPage => viewDrawing != null ? (float)scrollClientAreaSize.Height / (float)viewDrawing.LineHeight : 10;

		bool IView.HasInputFocus
		{
			get { return isFocused; }
		}

		void IView.ReceiveInputFocus()
		{
			InnerView.Window.MakeFirstResponder(InnerView);
		}

		string[] IViewFonts.AvailablePreferredFamilies
		{
			get
			{
				return new string[] { "Courier" };
			}
		}

		KeyValuePair<Appearance.LogFontSize, int>[] IViewFonts.FontSizes
		{
			get
			{
				return new []
				{
					new KeyValuePair<Settings.Appearance.LogFontSize, int>(Appearance.LogFontSize.Normal, 10)
				};
			}
		}

		void UpdateInnerViewSize()
		{
			InnerView.Frame = new CoreGraphics.CGRect(0, 0, viewWidth, scrollClientAreaSize.Height);
		}

		internal void OnPaint(RectangleF dirtyRect)
		{
			if (viewModel == null)
				return;

			var perfCountersWriter = drawingPerfCounters.GetWriter (
				atMostOncePer: TimeSpan.FromMilliseconds (500));
			using (perfCountersWriter.IncrementTicks (controlPaintTimeCounter)) {
				if (!perfCountersWriter.IsNull) {
					perfCountersWriter.Increment (controlPaintWidthCounter, (long)scrollClientAreaSize.Width);
					perfCountersWriter.Increment (controlPaintHeightCounter, (long)scrollClientAreaSize.Height);
				}

				graphicsForMeasurment.SetCurrentContext();
				using (var g = new LJD.Graphics()) {
					g.ConfigureProfiling (this.graphicsCounters, perfCountersWriter);
					g.EnableTextAntialiasing(false);

					viewDrawing.PaintControl(g, dirtyRect.ToRectangle (), isFocused, out var _);

					int maxRight = Math.Max (viewDrawing.MeasureView(), (int)scrollClientAreaSize.Width);
					if (maxRight > viewWidth || maxRight < 2*viewWidth/3) {
						viewWidth = maxRight;
						UpdateInnerViewSize ();
					}
				}
			}
			if (!perfCountersWriter.IsNull) {
				drawingPerfCounters.Report ();
				drawingPerfCounters.ResetAll ();
			}
		}

		internal void OnScrollWheel(NSEvent e)
		{
			bool isRegularMouseScroll = !e.HasPreciseScrollingDeltas;
			nfloat multiplier = isRegularMouseScroll ? 20 : 1;
			viewModel.OnIncrementalVScroll((float)(-multiplier * e.ScrollingDeltaY / viewDrawing.LineHeight));

			var pos = ScrollView.ContentView.Bounds.Location;
			InnerView.ScrollPoint(new CoreGraphics.CGPoint(pos.X - e.ScrollingDeltaX, pos.Y));
		}

		internal void OnMouseDown(NSEvent e)
		{
			MessageMouseEventFlag flags = MessageMouseEventFlag.None;
			if (e.Type == NSEventType.RightMouseDown)
				flags |= MessageMouseEventFlag.RightMouseButton;
			if ((e.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
				flags |= MessageMouseEventFlag.ShiftIsHeld;
			if ((e.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0)
				flags |= MessageMouseEventFlag.AltIsHeld;
			if ((e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				flags |= MessageMouseEventFlag.CtrlIsHeld;
			if (e.ClickCount == 2)
				flags |= MessageMouseEventFlag.DblClick;
			else
				flags |= MessageMouseEventFlag.SingleClick;

			viewDrawing?.HandleMouseDown(
				ClientRectangle,
				InnerView.ConvertPointFromView (e.LocationInWindow, null).ToPoint(),
				flags,
				out var _
			);
		}

		internal void OnMouseMove(NSEvent e, bool dragging)
		{
			viewDrawing?.HandleMouseMove(
				ClientRectangle,
				InnerView.ConvertPointFromView(e.LocationInWindow, null).ToPoint(),
				dragging,
				out var _
			);
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			viewModel.OnVScroll(VertScroller.DoubleValue, isRealtimeScroll: true);
		}

		Rectangle ClientRectangle
		{
			get { return ScrollView.ContentView.DocumentVisibleRect().ToRectangle(); }
		}

		class ContextMenuDelegate : NSMenuDelegate
		{
			public LogViewerControlAdapter owner;
		
			public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
			{
				
			}
			
			public override void MenuWillOpen (NSMenu menu)
			{
				menu.RemoveAllItems();
				var menuData = owner.viewModel.OnMenuOpening();
				foreach (var item in new Dictionary<ContextMenuItem, string>()
				{
					{ ContextMenuItem.ShowTime, "Show time" },
					{ ContextMenuItem.ShowRawMessages, "Show raw log" },
					{ ContextMenuItem.Copy, "Copy" },
					{ ContextMenuItem.ToggleBmk, "Toggle bookmark" },
					{ ContextMenuItem.GotoNextMessageInTheThread, "Go to next message in thread" },
					{ ContextMenuItem.GotoPrevMessageInTheThread, "Go to prev message in thread" },
					{ ContextMenuItem.DefaultAction, menuData.DefaultItemText },
				})
				{
					if ((menuData.VisibleItems & item.Key) == 0)
						continue;
					menu.AddItem(MakeItem(item.Key, item.Value, (menuData.CheckedItems & item.Key) != 0));
				}
				if (menuData.ExtendededItems != null && menuData.ExtendededItems.Count > 0)
				{
					menu.AddItem(NSMenuItem.SeparatorItem);
					foreach (var extItem in menuData.ExtendededItems)
						menu.AddItem(new NSMenuItem(extItem.Text,(sender, e) => extItem.Click()));
				}
			}
			
			NSMenuItem MakeItem(ContextMenuItem i, string title, bool isChecked)
			{
				var item = new NSMenuItem(title, (sender, e) => owner.viewModel.OnMenuItemClicked(i, !isChecked));
				if (isChecked)
					item.State = NSCellStateValue.On;
				return item;
			}
		};
	}
}

