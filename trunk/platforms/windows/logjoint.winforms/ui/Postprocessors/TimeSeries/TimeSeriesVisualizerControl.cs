using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesVisualizerControl : UserControl, IView
	{
		IViewEvents eventsHandler;

		public TimeSeriesVisualizerControl()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		PlotsViewMetrics IView.PlotsViewMetrics
		{
			get
			{
				return new PlotsViewMetrics()
				{
					Size = new SizeF(plotsPanel.Width, plotsPanel.Height)
				};
			}
		}

		void IView.Invalidate()
		{
			plotsPanel.Invalidate();
			yAxisPanel.Invalidate();
			xAxisPanel.Invalidate();
		}

		IConfigDialogView IView.CreateConfigDialogView(IConfigDialogEventsHandler evts)
		{
			var ret = new TimeSeriesVisualizerConfigDialog(evts);
			Application.OpenForms.OfType<Form>().FirstOrDefault()?.AddOwnedForm(ret);
			return ret;
		}

		private void plotsPanel_Paint(object sender, PaintEventArgs e)
		{
			using (var g = new LJD.Graphics(e.Graphics))
				DrawPlot(g, eventsHandler.OnDrawPlotsArea());
		}
	
		static void DrawMarker(LJD.Graphics g, LJD.Pen pen, PointF p)
		{
			float markerSize = 3; // todo: calc size on given platform
			g.DrawLine(pen, new PointF(p.X, p.Y - markerSize), new PointF(p.X, p.Y + markerSize));
			g.DrawLine(pen, new PointF(p.X - markerSize, p.Y), new PointF(p.X + markerSize, p.Y));
		}

		static void DrawPlot(LJD.Graphics g, PlotsDrawingData pdd)
		{
			g.PushState();
			g.EnableAntialiasing(true);
			foreach (var s in pdd.TimeSeries)
			{
				var pen = new LJD.Pen(s.Color.ToColor(), 1); // todo: cache pens
				var pts = s.Points.ToArray(); // todo: avoid array allocation. use DrawLine().
				if (pts.Length > 1)
					g.DrawLines(pen, pts);
				foreach (var p in pts)
					DrawMarker(g, pen, p);
			}
			g.PopState();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			KeyCode k = KeyCode.None;
			switch (keyData)
			{
				case Keys.Up: k = KeyCode.Up; break;
				case Keys.Down: k = KeyCode.Down; break;
				case Keys.Left: k = KeyCode.Left; break;
				case Keys.Right: k = KeyCode.Right; break;
				case Keys.F5: k = KeyCode.Refresh; break;
				case Keys.Add: k = KeyCode.Plus; break;
				case Keys.Subtract: k = KeyCode.Minus; break;
			}
			if (k != KeyCode.None)
			{
				eventsHandler.OnKeyDown(k);
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void plotsPanel_MouseDown(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseDown(new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseMove(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseMove(new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseUp(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseUp(new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseWheel(object sender, MouseEventArgs e)
		{
			float factor = 1.2f;
			eventsHandler.OnMouseZoom(new PointF(e.X, e.Y), e.Delta < 0 ? factor : 1f/factor);
		}

		private void configureViewLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnConfigViewClicked();
		}

		private void resetAxisLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnResetAxisClicked();
		}
	}
}
