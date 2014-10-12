using System.Drawing;
using LogJoint.UI;

namespace System.Windows.Forms
{
	public class ExtendedToolStrip : ToolStrip
	{
		public ExtendedToolStrip(): base()
		{
		}

		public ExtendedToolStrip(params ToolStripItem[] items): base(items) 
		{
		}

		public bool ResizingEnabled
		{
			get { return resizeRectangleEnabled; }
			set { resizeRectangleEnabled = value; }
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

			if (m.Msg == WM_MOUSEACTIVATE &&
				m.Result == (IntPtr)MA_ACTIVATEANDEAT)
			{
				base.WndProc(ref m);
				m.Result = (IntPtr)MA_ACTIVATE;
			}
			else if (m.Msg == WM_SETCURSOR && m.WParam == this.Handle)
			{
				var resizeRect = GetResizeRectangle();
				if (resizeRect.HasValue && resizeRect.Value.Contains(PointToClient(Cursor.Position)))
					Cursor.Current = Cursors.HSplit;
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
			Rectangle? resizeRect = GetResizeRectangle();
			if (resizeRect != null)
			{
				var r = resizeRect.Value;
				for (int dy = 0; dy < (r.Height - 3); dy += 6)
				{
					UIUtils.DrawDragEllipsis(e.Graphics, new Rectangle(r.X, r.Y + dy, r.Width, r.Height));
				}
			}
		}

		Rectangle? GetResizeRectangle()
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
			return new Rectangle(
				(x2 + x1 - resizeRectangleWidth) / 2,
				resizeRectangleVertPadding,
				resizeRectangleWidth,
				ClientSize.Height - (resizeRectangleVertPadding * 2)
			);
		}

		protected override void OnMouseDown(MouseEventArgs mea)
		{
			base.OnMouseDown(mea);
			if (GetResizeRectangle().GetValueOrDefault().Contains(mea.Location))
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

		private bool resizeRectangleEnabled;
		private const int resizeRectangleWidth = 80;
		private const int resizeRectangleVertPadding = 5;
		private int? resizeInitialCursorPositionY;
	}
}
