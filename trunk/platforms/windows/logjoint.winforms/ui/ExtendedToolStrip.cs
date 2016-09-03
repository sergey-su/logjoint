using System.Drawing;
using LogJoint.UI;

namespace System.Windows.Forms
{
	public class ExtendedToolStrip : ToolStrip
	{
		public ExtendedToolStrip(): base()
		{
			CtrCommon();
		}

		public ExtendedToolStrip(params ToolStripItem[] items): base(items) 
		{
			CtrCommon();
		}

		public bool ResizingEnabled
		{
			get { return resizeRectangleEnabled; }
			set { resizeRectangleEnabled = value; }
		}

		public static int ResizeRectangleWidth
		{
			get { return resizeRectangleWidth; }
		}

		public class ResizingEventArgs: EventArgs
		{
			public int Delta;
		};

		public event EventHandler ResizingStarted;
		public event EventHandler<ResizingEventArgs> Resizing;
		public event EventHandler ResizingFinished;

		protected override void WndProc(ref Message m)
		{
			int WM_MOUSEACTIVATE = 0x0021;
			int MA_ACTIVATEANDEAT = 2;
			int MA_ACTIVATE = 1;
			int WM_SETCURSOR = 0x0020;

			if (m.Msg == WM_MOUSEACTIVATE)
			{
				base.WndProc(ref m);
				// allow toolstrip be clicked with single click even if the app 
				// does not currently have the input focus
				if (m.Result == (IntPtr)MA_ACTIVATEANDEAT)
					m.Result = (IntPtr)MA_ACTIVATE;
			}
			else if (m.Msg == WM_SETCURSOR && m.WParam == this.Handle)
			{
				var resizeRect = GetResizeRectangle(forPainting: false);
				if (resizeRect.HasValue && resizeRect.Value.Contains(PointToClient(Cursor.Position)))
					Cursor.Current = Cursors.SizeNS;
				else
					base.WndProc(ref m);
			}
			else
			{
				base.WndProc(ref m);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Rectangle? resizeRect = GetResizeRectangle(forPainting: true);
			if (resizeRect != null)
			{
				var r = resizeRect.Value;
				for (int dy = 0; dy < (r.Height - 3); dy += 6)
				{
					UIUtils.DrawDragEllipsis(e.Graphics, new Rectangle(r.X, r.Y + dy, r.Width, r.Height));
				}
			}
		}

		Rectangle? GetResizeRectangle(bool forPainting)
		{
			if (!resizeRectangleEnabled)
				return null;
			ToolStripItem rightMostLeftAligned = null;
			ToolStripItem leftMostRightAligned = null;
			for (int itemIdx = 0; itemIdx < Items.Count; ++itemIdx)
			{
				var item = Items[itemIdx];
				if (!item.Visible)
					continue;
				if (item.Alignment == ToolStripItemAlignment.Left)
					if (rightMostLeftAligned == null || item.Bounds.Right > rightMostLeftAligned.Bounds.Right)
						rightMostLeftAligned = item;
				if (item.Alignment == ToolStripItemAlignment.Right)
					if (leftMostRightAligned == null || item.Bounds.Left < leftMostRightAligned.Bounds.Left)
						leftMostRightAligned = item;
			}
			int x1 = rightMostLeftAligned != null ? rightMostLeftAligned.Bounds.Right : 0;
			int x2 = leftMostRightAligned != null ? leftMostRightAligned.Bounds.Left : ClientSize.Width;
			if (x2 - x1 < resizeRectangleWidth)
				return null;
			if (forPainting)
				return new Rectangle(
					(x2 + x1 - resizeRectangleWidth) / 2,
					resizeRectangleVertPadding,
					resizeRectangleWidth,
					ClientSize.Height - (resizeRectangleVertPadding * 2)
				);
			else
				return new Rectangle(
					x1,
					resizeRectangleVertPadding,
					x2 - x1,
					ClientSize.Height - (resizeRectangleVertPadding * 2)
				);
		}

		protected override void OnMouseDown(MouseEventArgs mea)
		{
			base.OnMouseDown(mea);
			if (GetResizeRectangle(forPainting: false).GetValueOrDefault().Contains(mea.Location))
			{
				resizeInitialCursorPositionY = Cursor.Position.Y;
				Capture = true;
				if (ResizingStarted != null)
					ResizingStarted(this, EventArgs.Empty);
			}
		}

		protected override void OnMouseMove(MouseEventArgs mea)
		{
			base.OnMouseMove(mea);
			if (resizeInitialCursorPositionY.HasValue)
			{
				if (Resizing != null)
					Resizing(this, new ResizingEventArgs() { Delta = Cursor.Position.Y - resizeInitialCursorPositionY.Value });
			}
		}

		protected override void OnMouseUp(MouseEventArgs mea)
		{
			base.OnMouseUp(mea);
			if (resizeInitialCursorPositionY != null)
			{
				Capture = false;
				resizeInitialCursorPositionY = null;
				if (ResizingFinished != null)
					ResizingFinished(this, EventArgs.Empty);
			}
		}

		void CtrCommon()
		{
			this.Renderer = new NoBorderRenderer();
		}

		public class NoBorderRenderer : ToolStripSystemRenderer
		{
			protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
			{
				if (e.ToolStrip is ToolStripDropDownMenu)
				{
					base.OnRenderToolStripBorder(e);
				}
			}
		}

		private bool resizeRectangleEnabled;
		private const int resizeRectangleWidth = 80;
		private const int resizeRectangleVertPadding = 5;
		private int? resizeInitialCursorPositionY;
	}
}
