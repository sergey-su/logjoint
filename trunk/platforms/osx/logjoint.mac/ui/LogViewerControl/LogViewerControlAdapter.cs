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

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSResponder, IView
	{
		internal IViewModel viewEvents;
		internal bool isFocused;
		NSTimer animationTimer;
		string drawDropMessage;
		bool enableCursor = true;

		[Export("innerView")]
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
			InitDrawingContext();
			InitCursorTimer();
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

		void InitCursorTimer()
		{
			NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(500), _ =>
			{
				drawContext.CursorState = !drawContext.CursorState;
				if (viewEvents != null)
					viewEvents.OnCursorTimerTick();
			});
		}

		void InitScrollView()
		{
			// without this vert. scrolling with touch gesture often gets stuck
			// if gesture is not absolulety vertical
			ScrollView.HorizontalScrollElasticity = NSScrollElasticity.None;

			ScrollView.PostsFrameChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, async ns =>
			{
				if (viewEvents != null)
					viewEvents.OnDisplayLinesPerPageChanged();
				await Task.Yield(); // w/o this hack inner view is never painted until first resize
				UpdateInnerViewSize();
			}, ScrollView);

			VertScroller.Action = new Selector ("OnVertScrollChanged");
			VertScroller.Target = this;
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

		#region IView implementation

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewEvents = viewModel;
			this.drawContext.Presenter = viewModel;
			var updater = Updaters.Create (
				() => viewModel.HighlightingFiltersHandler,
				() => viewModel.SelectionHighlightingHandler,
				() => viewModel.SearchResultHighlightingHandler,
				(_1, _2, _3) => { this.InnerView.NeedsDisplay = true; }
			);
			viewModel.ChangeNotification.CreateSubscription (updater);
		}

		void IView.UpdateFontDependentData(string fontName, Appearance.LogFontSize fontSize)
		{
			if (drawContext.Font != null)
				drawContext.Font.Dispose();
			
			drawContext.Font = new LJD.Font("monaco", 12);

			using (var tmp = new LJD.Graphics()) // todo: consider reusing with windows
			{
				int count = 8 * 1024;
				drawContext.CharSize = tmp.MeasureString(new string('0', count), drawContext.Font);
				drawContext.CharWidthDblPrecision = (double)drawContext.CharSize.Width / (double)count;
				drawContext.CharSize.Width /= (float)count;
				drawContext.LineHeight = (int)Math.Floor(drawContext.CharSize.Height);
			}

			UpdateTimeAreaSize();
		}

		void IView.HScrollToSelectedText(SelectionInfo selection)
		{
			if (selection.First.Message == null)
				return;

			int pixelThatMustBeVisible = (int)(selection.First.LineCharIndex * drawContext.CharSize.Width);
			if (drawContext.ShowTime)
				pixelThatMustBeVisible += drawContext.TimeAreaSize;

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

		object IView.GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection)
		{
			// todo
			return null;
		}

		void IView.PopupContextMenu(object contextMenuPopupData)
		{
			// todo
		}

		void IView.Invalidate()
		{
			InnerView.NeedsDisplay = true;
		}

		void IView.InvalidateLine(ViewLine line)
		{
			Rectangle r = DrawingUtils.GetMetrics(line, drawContext).MessageRect;
			InnerView.SetNeedsDisplayInRect(r.ToRectangleF().ToCGRect ());
		}

		void IView.DisplayNothingLoadedMessage(string messageToDisplayOrNull)
		{
			drawDropMessage = messageToDisplayOrNull;
			DragDropIconView.Hidden = messageToDisplayOrNull == null;
		}

		void IView.RestartCursorBlinking()
		{
			drawContext.CursorState = true;
		}

		void IView.UpdateMillisecondsModeDependentData()
		{
			UpdateTimeAreaSize();
		}

		void IView.AnimateSlaveMessagePosition()
		{
			drawContext.SlaveMessagePositionAnimationStep = 0;
			InnerView.NeedsDisplay = true;
			if (animationTimer != null)
				animationTimer.Dispose();
			animationTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(50), _ =>
			{
				if (drawContext.SlaveMessagePositionAnimationStep < 8)
				{
					drawContext.SlaveMessagePositionAnimationStep++;
				}
				else
				{
					animationTimer.Dispose();
					animationTimer = null;
					drawContext.SlaveMessagePositionAnimationStep = 0;
				}
				InnerView.NeedsDisplay = true;
			});
		}

		float IView.DisplayLinesPerPage { get { return (float)ScrollView.Frame.Height / (float)drawContext.LineHeight; } }

		void IView.SetVScroll(double? value)
		{
			VertScroller.Enabled = value.HasValue;
			VertScroller.KnobProportion = 0.0001f;
			VertScroller.DoubleValue = value.GetValueOrDefault();
		}

		bool IView.HasInputFocus
		{
			get { return isFocused; }
		}

		void IView.ReceiveInputFocus()
		{
			InnerView.Window.MakeFirstResponder(InnerView);
		}

		#endregion

		#region IViewFonts implementation

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

		#endregion

		void UpdateInnerViewSize()
		{
			InnerView.Frame = new CoreGraphics.CGRect(0, 0, viewWidth, ScrollView.Frame.Height);
		}

		internal void OnPaint(RectangleF dirtyRect)
		{
			if (viewEvents == null)
				return;
			
			UpdateClientSize();

			drawContext.Canvas = new LJD.Graphics();
			drawContext.ScrollPos = new Point(0,
				(int)(viewEvents.GetFirstDisplayMessageScrolledLines() * (double)drawContext.LineHeight));

			int maxRight;
			DrawingUtils.PaintControl(drawContext, viewEvents, selection, isFocused, 
				dirtyRect.ToRectangle(), out maxRight);

			if (maxRight > viewWidth)
			{
				viewWidth = maxRight;
				UpdateInnerViewSize();
			}
		}

		internal void OnScrollWheel(NSEvent e)
		{
			bool isRegularMouseScroll = !e.HasPreciseScrollingDeltas;
			nfloat multiplier = isRegularMouseScroll ? 20 : 1;
			viewEvents.OnIncrementalVScroll((float)(-multiplier * e.ScrollingDeltaY / drawContext.LineHeight));

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
			
			bool captureTheMouse;

			DrawingUtils.MouseDownHelper(
				viewEvents,
				drawContext,
				ClientRectangle,
				viewEvents,
				InnerView.ConvertPointFromView (e.LocationInWindow, null).ToPoint(),
				flags,
				out captureTheMouse
			);
		}

		internal void OnMouseMove(NSEvent e, bool dragging)
		{
			DrawingUtils.CursorType cursor;
			DrawingUtils.MouseMoveHelper(
				viewEvents,
				drawContext,
				ClientRectangle,
				viewEvents,
				InnerView.ConvertPointFromView(e.LocationInWindow, null).ToPoint(),
				dragging,
				out cursor
			);
		}

		void InitDrawingContext()
		{
			drawContext.DefaultBackgroundBrush = new LJD.Brush(Color.White);
			drawContext.InfoMessagesBrush = new LJD.Brush(Color.Black);
			drawContext.CommentsBrush = new LJD.Brush(Color.Gray);
			drawContext.SelectedBkBrush = new LJD.Brush(Color.FromArgb(167, 176, 201));
			//drawContext.SelectedFocuslessBkBrush = new LJD.Brush(Color.Gray);
			drawContext.CursorPen = new LJD.Pen(Color.Black, 2);
			drawContext.TimeSeparatorLine = new LJD.Pen(Color.Gray, 1);

			int hightlightingAlpha = 170;
			drawContext.SearchResultHighlightingBackground =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			drawContext.SelectionHighlightingBackground =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));
			
			drawContext.ErrorIcon = new LJD.Image(NSImage.ImageNamed("ErrorLogSeverity.png"));
			drawContext.WarnIcon = new LJD.Image(NSImage.ImageNamed("WarnLogSeverity.png"));
			drawContext.BookmarkIcon = new LJD.Image(NSImage.ImageNamed("Bookmark.png"));
			drawContext.SmallBookmarkIcon = new LJD.Image(NSImage.ImageNamed("Bookmark.png"));
			drawContext.FocusedMessageIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsg.png"));
			drawContext.FocusedMessageSlaveIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlave.png"));
		}

		void UpdateClientSize()
		{
			drawContext.ViewWidth = viewWidth;
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			viewEvents.OnVScroll(VertScroller.DoubleValue, isRealtimeScroll: true);
		}

		private static int ToFontEmSize(LogFontSize fontSize) // todo: review sizes
		{
			switch (fontSize)
			{
				case LogFontSize.SuperSmall: return 6;
				case LogFontSize.ExtraSmall: return 7;
				case LogFontSize.Small: return 8;
				case LogFontSize.Large1: return 10;
				case LogFontSize.Large2: return 11;
				case LogFontSize.Large3: return 14;
				case LogFontSize.Large4: return 16;
				case LogFontSize.Large5: return 18;
				default: return 14;
			}
		}

		void UpdateTimeAreaSize()
		{
			string testStr = (new MessageTimestamp(new DateTime(2011, 11, 11, 11, 11, 11, 111))).ToUserFrendlyString(drawContext.ShowMilliseconds);
			drawContext.TimeAreaSize = (int)Math.Floor(
				drawContext.CharSize.Width * (float)testStr.Length
			) + 10;
		}

		SelectionInfo selection { get { return viewEvents != null ? viewEvents.Selection : new SelectionInfo(); } }

		Rectangle ClientRectangle
		{
			get { return ScrollView.ContentView.DocumentVisibleRect().ToRectangle(); }
		}

		internal DrawContext DrawContext { get { return drawContext; } }

		class ContextMenuDelegate : NSMenuDelegate
		{
			public LogViewerControlAdapter owner;
		
			public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
			{
				
			}
			
			public override void MenuWillOpen (NSMenu menu)
			{
				menu.RemoveAllItems();
				var menuData = owner.viewEvents.OnMenuOpening();
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
				var item = new NSMenuItem(title, (sender, e) => owner.viewEvents.OnMenuItemClicked(i, !isChecked));
				if (isChecked)
					item.State = NSCellStateValue.On;
				return item;
			}
		};

		DrawContext drawContext = new DrawContext();
		int viewWidth = 2000;
	}
}

