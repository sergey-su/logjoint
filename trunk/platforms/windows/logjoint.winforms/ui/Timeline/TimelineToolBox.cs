using System;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelineToolBox : UserControl
	{
		public TimelineToolBox()
		{
			InitializeComponent();

			toolStrip1.ImageScalingSize = new Size(UIUtils.Dpi.Scale(14), UIUtils.Dpi.Scale(14));
			zoomInToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.ZoomIn, toolStrip1.ImageScalingSize);
			zoomOutToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.ZoomOut, toolStrip1.ImageScalingSize);
			zoomToViewAllToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.ZoomReset, toolStrip1.ImageScalingSize);
			scrollUpToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.MoveUp, toolStrip1.ImageScalingSize);
			scrollDownToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.MoveDown, toolStrip1.ImageScalingSize);

			zoomInToolStripButton.Tag = (Action)(() => { presenter.OnZoomToolButtonClicked(1); });
			zoomOutToolStripButton.Tag = (Action)(() => { presenter.OnZoomToolButtonClicked(-1); });
			zoomToViewAllToolStripButton.Tag = (Action)(() => { presenter.OnZoomToViewAllToolButtonClicked(); });
			scrollUpToolStripButton.Tag = (Action)(() => { presenter.OnScrollToolButtonClicked(1); });
			scrollDownToolStripButton.Tag = (Action)(() => { presenter.OnScrollToolButtonClicked(-1); });
		}

		public void SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		static void ExecAction(object button)
		{
			((Action)(((ToolStripButton)button).Tag))();
		}

		private void toolButtonClick(object sender, EventArgs e)
		{
			if (repeatitionState == RepeatitionState.None)
				ExecAction(sender);
		}

		private void toolButtonMouseDown(object sender, MouseEventArgs e)
		{
			ExecAction(sender);
			if (sender == zoomToViewAllToolStripButton)
				SetNoneState();
			else
				SetInitialWaitingState((ToolStripButton)sender);
		}

		private void toolButtonMouseUp(object sender, MouseEventArgs e)
		{
			SetNoneState();
		}

		void SetInitialWaitingState(ToolStripButton button)
		{
			currentButton = button;
			repeatitionState = RepeatitionState.InitialWaiting;
			repeatTimer.Interval = 300;
			repeatTimer.Enabled = true;
		}

		void SetNoneState()
		{
			currentButton = null;
			repeatitionState = RepeatitionState.None;
			repeatTimer.Enabled = false;
		}

		void SetRepeatingState()
		{
			repeatitionState = RepeatitionState.Repeating;
			repeatTimer.Enabled = true;
			repeatTimer.Interval = 100;
		}

		private void repeatTimer_Tick(object sender, EventArgs e)
		{
			switch (repeatitionState)
			{
				case RepeatitionState.InitialWaiting:
					SetRepeatingState();
					break;
				case RepeatitionState.Repeating:
					ExecAction(currentButton);
					break;
			}
		}

		void toolStrip1_MouseCaptureChanged(object sender, EventArgs e)
		{
			if (!toolStrip1.Capture)
				SetNoneState();
		}

		ToolStripButton currentButton;
		enum RepeatitionState { None, InitialWaiting, Repeating };
		RepeatitionState repeatitionState;

		IViewEvents presenter;
	}
}
