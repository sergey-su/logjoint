using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using LJD = LogJoint.Drawing;
using System.Collections.Generic;
using System;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesVisualizerControl : UserControl, IView
	{
		IViewEvents eventsHandler;
		readonly Drawing.Resources resources = new Drawing.Resources("Tahoma", 7);

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
			get { return GetPlotViewMetrics(); }
		}

		void IView.Invalidate()
		{
			plotsPanel.Invalidate();
			yAxisPanel.Invalidate();
			xAxisPanel.Invalidate();
		}

		void IView.UpdateYAxesSize()
		{
			using (var g = new LJD.Graphics(yAxisPanel.CreateGraphics(), ownsGraphics: true))
			{
				var width = Drawing.GetYAxesMetrics(g, resources, eventsHandler.OnDrawPlotsArea()).Select(x => x.Width).Sum();
				mainLayoutPanel.ColumnStyles[mainLayoutPanel.GetColumn(yAxisPanel)] = 
					new ColumnStyle(SizeType.Absolute, Math.Max(width, mainLayoutPanel.Margin.Right));
			}
		}

		void IView.UpdateLegend(IEnumerable<LegendItemInfo> items)
		{
			var existingControls = legendFlowLayoutPanel.Controls.OfType<Control>().ToDictionary(c => (LegendItemInfo)c.Tag);
			foreach (var c in existingControls.Values)
			{
				c.Invalidate();
			}
			foreach (var item in items)
			{
				if (existingControls.Remove(item))
					continue;
				var label = new Label()
				{
					Tag = item,
					AutoSize = true,
					Text = item.Label,
					Margin = new Padding(5, 0, 5, 0),
					Padding = new Padding(30, 0, 0, 0)
				};
				label.Paint += legendLabel_Paint;
				label.DoubleClick += legendLabel_DoubleClick; ;
				legendFlowLayoutPanel.Controls.Add(label);
			}
			foreach (var ctrl in existingControls.Values)
				ctrl.Dispose();
			legendFlowLayoutPanel.Visible = legendFlowLayoutPanel.Controls.Count > 0;
		}

		IConfigDialogView IView.CreateConfigDialogView(IConfigDialogEventsHandler evts)
		{
			var ret = new TimeSeriesVisualizerConfigDialog(evts, resources);
			Application.OpenForms.OfType<Form>().FirstOrDefault()?.AddOwnedForm(ret);
			this.ParentForm?.AddOwnedForm(ret);
			return ret;
		}

		private void legendLabel_Paint(object sender, PaintEventArgs e)
		{
			var ctrl = (Control)sender;
			var data = (LegendItemInfo)ctrl.Tag;
			int w = ctrl.Padding.Left;
			int pad = 3;
			using (var g = new LJD.Graphics(e.Graphics))
				Drawing.DrawLegendSample(g, resources, data.Color, data.Marker, new RectangleF(pad, pad, w - pad * 2, ctrl.Height - pad * 2));
		}

		private void legendLabel_DoubleClick(object sender, System.EventArgs e)
		{
			eventsHandler.OnLegendItemDoubleClicked((LegendItemInfo)((Control)sender).Tag);
		}

		private void plotsPanel_Paint(object sender, PaintEventArgs e)
		{
			using (var g = new LJD.Graphics(e.Graphics))
				Drawing.DrawPlotsArea(g, resources, eventsHandler.OnDrawPlotsArea(), GetPlotViewMetrics());
		}

		private void xAxisPanel_Paint(object sender, PaintEventArgs e)
		{
			using (var g = new LJD.Graphics(e.Graphics))
				Drawing.DrawXAxis(g, resources, eventsHandler.OnDrawPlotsArea(), xAxisPanel.Height);
		}

		private PlotsViewMetrics GetPlotViewMetrics()
		{
			return new PlotsViewMetrics()
			{
				Size = new SizeF(plotsPanel.Width, plotsPanel.Height)
			};
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

		ViewPart GetViewPart(object sender, Point pt)
		{
			if (sender == plotsPanel)
				return new ViewPart()
				{
					Part = ViewPart.PartId.Plots
				};
			else if (sender == xAxisPanel)
				return new ViewPart()
				{
					Part = ViewPart.PartId.XAxis,
					AxisId = eventsHandler.OnDrawPlotsArea().XAxis.Id,
				};
			else if (sender == yAxisPanel)
				using (var g = new LJD.Graphics(yAxisPanel.CreateGraphics(), ownsGraphics: true))
					return new ViewPart()
					{
						Part = ViewPart.PartId.YAxis,
						AxisId = Drawing.GetYAxisId(g, resources, eventsHandler.OnDrawPlotsArea(), pt.X, yAxisPanel.Width)
					};
			return new ViewPart();
		}

		private void plotsPanel_MouseDown(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseDown(GetViewPart(sender, e.Location), new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseMove(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseMove(GetViewPart(sender, e.Location), new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseUp(object sender, MouseEventArgs e)
		{
			eventsHandler.OnMouseUp(GetViewPart(sender, e.Location), new PointF(e.X, e.Y));
		}

		private void plotsPanel_MouseWheel(object sender, MouseEventArgs e)
		{
			float factor = 1.2f;
			eventsHandler.OnMouseZoom(GetViewPart(sender, e.Location), new PointF(e.X, e.Y), e.Delta < 0 ? factor : 1f/factor);
		}

		private void configureViewLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnConfigViewClicked();
		}

		private void resetAxisLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnResetAxesClicked();
		}

		private void yAxisPanel_Paint(object sender, PaintEventArgs e)
		{
			using (var g = new LJD.Graphics(e.Graphics))
				Drawing.DrawYAxes(g, resources, eventsHandler.OnDrawPlotsArea(), yAxisPanel.Width, GetPlotViewMetrics());
		}
	}
}
