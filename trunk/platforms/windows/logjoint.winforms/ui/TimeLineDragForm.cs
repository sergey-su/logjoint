using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class TimeLineDragForm : Form
	{
		TimeLineControl timeLineControl;

		public TimeLineDragForm(TimeLineControl timeLineControl)
		{
			this.timeLineControl = timeLineControl;
			InitializeComponent();
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		public DateTime Date
		{
			get { return date; }
			set { date = value; Invalidate(); }
		}

		public TimeLineControl.DragArea Area
		{
			get { return area; }
			set { area = value; Invalidate(); }
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(SystemBrushes.ButtonFace, e.ClipRectangle);
			int h = TimeLineControl.DragAreaHeight;
			UIUtils.DrawDragEllipsis(e.Graphics, new Rectangle(
				h / 2, Area == TimeLineControl.DragArea.Top ? 0 : Height - h,
				Width - h, h));
			using (StringFormat fmt = new StringFormat())
			{
				fmt.Alignment = StringAlignment.Center;
				e.Graphics.DrawString(timeLineControl.GetUserFriendlyFullDateTimeString(date),
						timeLineControl.Font, Brushes.Black, Width / 2,
					Area == TimeLineControl.DragArea.Top ? h : 0, fmt);
			}
			e.Graphics.DrawRectangle(Pens.Gray, -1, -1, Width, Height);
			base.OnPaint(e);
		}

		DateTime date;
		TimeLineControl.DragArea area;
	}
}