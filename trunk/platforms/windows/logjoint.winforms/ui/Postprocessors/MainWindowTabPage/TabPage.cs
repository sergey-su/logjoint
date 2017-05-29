using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace LogJoint.UI.Postprocessing.MainWindowTabPage
{
	public partial class TabPage : UserControl, IView
	{
		IViewEvents eventsHandler;
		readonly Dictionary<ViewControlId, PostprocessorControls> postprocessorsControls = new Dictionary<ViewControlId, PostprocessorControls>();

		public TabPage(UI.Presenters.MainForm.IPresenter mainFormPresenter)
		{
			InitializeComponent();
			this.SetStyle(ControlStyles.Selectable, true);
			this.Dock = DockStyle.Fill;

			postprocessorsControls[ViewControlId.StateInspector] = new PostprocessorControls(stateInspectorLinkLabel, stateInspectorLinkLabel, stateInspectorProgressBar);
			postprocessorsControls[ViewControlId.Sequence] = new PostprocessorControls(sequenceDiagramLinkLabel, sequenceDiagramLinkLabel, sequenceDiagramProgressBar);
			postprocessorsControls[ViewControlId.TimeSeries] = new PostprocessorControls(timeSeriesLinkLabel, timeSeriesLinkLabel, timeSeriesProgressBar);
			postprocessorsControls[ViewControlId.Timeline] = new PostprocessorControls(timelineLinkLabel, timelineLinkLabel, timelineProgressBar);
			postprocessorsControls[ViewControlId.Correlate] = new PostprocessorControls(null, correlationAction1LinkLabel, correlationProgressBar);

			postprocessorsControls[ViewControlId.LogsCollectionControl1] = new PostprocessorControls(null, logsCollectionControlLinkLabel1, logsCollectionControlProgressBar1);
			postprocessorsControls[ViewControlId.LogsCollectionControl2] = new PostprocessorControls(null, logsCollectionControlLinkLabel2, logsCollectionControlProgressBar2);
			postprocessorsControls[ViewControlId.LogsCollectionControl3] = new PostprocessorControls(null, logsCollectionControlLinkLabel3, null);

			postprocessorsControls[ViewControlId.AllPostprocessors] = new PostprocessorControls(runAllPostprocessorsLinkLabel, runAllPostprocessorsLinkLabel, allPostprocessorsProgressBar);

			foreach (var x in postprocessorsControls)
			{
				x.Value.link.LinkClicked += (s, e) =>
				{
					if (!string.IsNullOrEmpty(e.Link.LinkData as string))
						eventsHandler.OnActionClick((string)e.Link.LinkData, x.Key, GetClickFlags());
				};
			}

			// todo: create when there a least one postprocessor exists. Postprocessors may come from plugings or it can be internal trace.
			// todo: delete page on stop

			mainFormPresenter.AddCustomTab(this, "Postprocessing", this);
			mainFormPresenter.TabChanging += (sender, e) =>
			{
				if (e.CustomTabTag == this)
				try
				{
					eventsHandler.OnTabPageSelected();
				}
				catch (Exception ex)
				{
					// todo: report
				}
			};
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.UpdateControl(ViewControlId viewId, ControlData data)
		{
			PostprocessorControls controls;
			if (!postprocessorsControls.TryGetValue(viewId, out controls))
				return;
			if (controls.container != null)
			{
				SetEnabled(controls.container, !data.Disabled);
			}
			if (viewId.IsLogsCollectionControl())
			{
				UIUtils.SetLinkContents(controls.link, data.Content);
			}
			else
			{
				UIUtils.SetLinkContents(controls.link, data.Content);
				SetColor(controls.link, GetColor(data.Color));
			}
			if (controls.progress != null)
			{
				controls.progress.Visible = data.Progress != null;
				if (data.Progress != null)
					controls.progress.Value = Math.Max(0, Math.Min(100, (int)(data.Progress.GetValueOrDefault(0) * 100)));
			}
		}

		void IView.BeginBatchUpdate()
		{
		}

		async void IView.EndBatchUpdate()
		{
			await Task.Yield();
			if (logsCollectionControlProgressBar2.Visible)
				MoveProgressBar(logsCollectionControlProgressBar2, logsCollectionControlLinkLabel2);
			if (logsCollectionControlProgressBar1.Visible)
				MoveProgressBar(logsCollectionControlProgressBar1, logsCollectionControlLinkLabel1);
		}

		private static void MoveProgressBar(ProgressBar progress, LinkLabel refCtrl)
		{
			progress.BringToFront();
			progress.Location = progress.Parent.PointToClient(
				refCtrl.PointToScreen(new Point(
					refCtrl.Width - progress.Width - progress.Margin.Right,
					refCtrl.Height - progress.Height - progress.Margin.Bottom
				))
			);
		}


		private void runAllPostprocessorsCellFiller_Paint(object sender, PaintEventArgs e)
		{
			Control ctr = (Control)sender;
			var sz = new SizeF(ctr.Width - 1, ctr.Height - 1);
			e.Graphics.DrawLines(
				Pens.Gray,
				new[]
				{
					new PointF(0, sz.Height/1.5f),
					new PointF(0, sz.Height),
					new PointF(sz.Width, sz.Height),
					new PointF(sz.Width, sz.Height/1.5f),
				}
			);
		}

		static Color GetColor(ControlData.StatusColor statusColor)
		{
			switch (statusColor)
			{
				case ControlData.StatusColor.Error: return Color.Red;
				case ControlData.StatusColor.Warning: return Color.Salmon;
				case ControlData.StatusColor.Success: return Color.FromArgb(0, 176, 80);
				default: return Color.Black;
			}
		}

		static ClickFlags GetClickFlags()
		{
			var ret = ClickFlags.None;
			if (Control.ModifierKeys != 0)
				ret |= ClickFlags.AnyModifier;
			return ret;
		}

		static void SetEnabled(Control ctrl, bool value)
		{
			if (ctrl.Enabled != value)
				ctrl.Enabled = value;
		}

		static void SetText(Label l, string value)
		{
			if (l.Text == value)
				return;
			l.Text = value;
		}

		static void SetColor(Label l, Color value)
		{
			if (l.ForeColor == value)
				return;
			l.ForeColor = value;
		}

		class PostprocessorControls
		{
			readonly public Control container;
			readonly public LinkLabel link;
			readonly public ProgressBar progress;

			public PostprocessorControls(Control container, LinkLabel link, ProgressBar progress)
			{
				this.container = container;
				this.link = link;
				this.progress = progress;
			}
		};
	}
}
