using LogJoint.UI.Presenters.Timeline;
using LogJoint.UI.Timeline;
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

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DateTime Date
		{
			get { return date; }
			set { date = value; Invalidate(); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ViewArea Area
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
			int h = StaticMetrics.DragAreaHeight;
			UIUtils.DrawDragEllipsis(e.Graphics, new Rectangle(
				h / 2, Area == ViewArea.TopDrag ? 0 : Height - h,
				Width - h, h));
			timeLineControl.DrawDragArea(e.Graphics, date, 0, Width, Area == ViewArea.TopDrag ? h : 0);
			e.Graphics.DrawRectangle(Pens.Gray, -1, -1, Width, Height);
			base.OnPaint(e);
		}

		DateTime date;
		ViewArea area;
	}
}