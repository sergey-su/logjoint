using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.Drawing;
using System.Drawing;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class LegendItemController : NSCollectionViewItem
	{
		LegendItemInfo item;
		Drawing.Resources resources;
		IViewEvents events;

		public new LegendItemView View
		{
			get
			{
				return (LegendItemView)base.View;
			}
		}

		public void Init(LegendItemInfo item, Drawing.Resources resources, IViewEvents events)
		{
			this.item = item;
			this.resources = resources;
			this.events = events;
			previewView.NeedsDisplay = true;
		}

		#region Constructors
		// Called when created from unmanaged code
		public LegendItemController(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public LegendItemController(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		// Call to load from the XIB/NIB file
		public LegendItemController() : base("LegendItemView", NSBundle.MainBundle)
		{
			Initialize();
		}

		// Added to support loading from XIB/NIB
		public LegendItemController(string nibName, NSBundle nibBundle) : base(nibName, nibBundle) {

			Initialize();
		}

		// Shared initialization code
		void Initialize()
		{
		}
		#endregion

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			previewView.OnPaint = (r) =>
			{
				if (item == null)
					return;
				using (var g = new Graphics())
				{
					var bnds = previewView.Bounds.ToRectangleF();
					var previewRect = new RectangleF(
						bnds.X, bnds.Y, bnds.X + 40, bnds.Height);
					Drawing.DrawLegendSample(g, resources, item.Color, item.Marker, previewRect);
					var labelRect = new RectangleF(
						previewRect.Right + 3, bnds.Top, 
						bnds.Width - 3, bnds.Height);
					var sf = new StringFormat(StringAlignment.Near, StringAlignment.Center);
					g.DrawString(item.Label, resources.AxesFont, Brushes.Black, labelRect, sf);
				}
			};
			previewView.OnMouseMove = e => NSCursor.PointingHandCursor.Set ();
			previewView.OnMouseLeave = e => NSCursor.ArrowCursor.Set ();
			previewView.OnMouseDown = (NSEvent e) =>
			{
				if (item != null && events != null)
				{
					events.OnLegendItemClicked(item);
				}
			};
		}
	}
}
