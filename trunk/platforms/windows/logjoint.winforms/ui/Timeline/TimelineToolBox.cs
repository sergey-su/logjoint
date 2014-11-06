using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelineToolBox : UserControl
	{
		public TimelineToolBox()
		{
			InitializeComponent();

			zoomInToolStripButton.Tag = (Action)(() => { presenter.OnZoomToolButtonClicked(1); });
			zoomOutToolStripButton.Tag = (Action)(() => { presenter.OnZoomToolButtonClicked(-1); });
			zoomToViewAllToolStripButton.Tag = (Action)(() => { presenter.OnZoomToViewAllToolButtonClicked(); });
			scrollUpToolStripButton.Tag = (Action)(() => { presenter.OnScrollToolButtonClicked(1); });
			scrollDownToolStripButton.Tag = (Action)(() => { presenter.OnScrollToolButtonClicked(-1); });
			viewTailModeToolStripButton.Tag = (Action)(() => { presenter.OnViewTailModeToolButtonClicked(!viewTailModeToolStripButton.Checked); });
		}

		public void SetPresenter(IPresenterEvents presenter)
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
			if (sender == zoomToViewAllToolStripButton || sender == viewTailModeToolStripButton)
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

		IPresenterEvents presenter;
	}
}
