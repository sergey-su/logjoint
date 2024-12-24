using LogJoint.UI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class DoubleBufferedPanel : Panel
	{
		UIUtils.FocuslessMouseWheelMessagingFilter focuslessMouseWheelMessagingFilter;

		public DoubleBufferedPanel()
		{
			InitializeComponent();
			SetStyle(
				ControlStyles.ResizeRedraw | 
				ControlStyles.OptimizedDoubleBuffer | 
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.Selectable, true);
			//DisplayPaintTime = true;
			this.Disposed += (s, e) => { if (focuslessMouseWheelMessagingFilter != null) focuslessMouseWheelMessagingFilter.Dispose(); };
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public new event KeyPressEventHandler KeyPress
		{
			add { base.KeyPress += value; }
			remove { base.KeyPress -= value; }
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public new event MouseEventHandler MouseWheel
		{
			add { base.MouseWheel += value; }
			remove { base.MouseWheel -= value; }
		}

		[Category("Mouse")]
		public event EventHandler<HandledMouseEventArgs> SetCursor;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DisplayPaintTime { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool FocuslessMouseWheel
		{
			get { return focuslessMouseWheelMessagingFilter != null; }
			set
			{
				if (value == FocuslessMouseWheel)
					return;
				if (value)
				{
					focuslessMouseWheelMessagingFilter = new UIUtils.FocuslessMouseWheelMessagingFilter(this);
				}
				else
				{
					focuslessMouseWheelMessagingFilter.Dispose();
					focuslessMouseWheelMessagingFilter = null;
				}
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			Stopwatch sw = DisplayPaintTime ? Stopwatch.StartNew() : null;
			using (var b = new SolidBrush(this.BackColor))
				pe.Graphics.FillRectangle(b, pe.ClipRectangle);
			base.OnPaint(pe);
			if (sw != null)
			{
				sw.Stop();
				int y = Height / 2;
				pe.Graphics.FillRectangle(Brushes.WhiteSmoke, new Rectangle(0, y, 100, 30));
				if (sw.Elapsed.Ticks != 0)
				{
					pe.Graphics.DrawString(
						string.Format("fps: {0}", 1.0 / sw.Elapsed.TotalSeconds),
						Font, Brushes.Black, new PointF(0, y));
				}
			}
		}

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			const int WM_SETCURSOR = 0x0020;
			switch (m.Msg)
			{
				case WM_SETCURSOR:
					if (!WmSetCursor(ref m))
						base.WndProc(ref m);
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		private bool WmSetCursor(ref System.Windows.Forms.Message m)
		{
			if (SetCursor != null)
			{
				var pos = PointToClient(Control.MousePosition);
				var args = new HandledMouseEventArgs(Control.MouseButtons, 0, pos.X, pos.Y, 0, false);
				SetCursor(this, args);
				return args.Handled;
			}
			return false;
		}
	}
}
