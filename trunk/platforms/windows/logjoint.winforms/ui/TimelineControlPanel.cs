using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class TimelineControlPanel : UserControl
	{
		public TimelineControlPanel()
		{
			InitializeComponent();

			zoomInToolStripButton.Tag = (Action)(() => { if (Zoom != null) Zoom(this, new TimelineControlEventArgs(1)); });
			zoomOutToolStripButton.Tag = (Action)(() => { if (Zoom != null) Zoom(this, new TimelineControlEventArgs(-1)); });
			zoomToViewAllToolStripButton.Tag = (Action)(() => { if (ZoomToViewAll != null) ZoomToViewAll(this, EventArgs.Empty); });
			scrollUpToolStripButton.Tag = (Action)(() => { if (Scroll != null) Scroll(this, new TimelineControlEventArgs(1)); });
			scrollDownToolStripButton.Tag = (Action)(() => { if (Scroll != null) Scroll(this, new TimelineControlEventArgs(-1)); });
			viewTailModeToolStripButton.Tag = (Action)(() => { if (ViewTailMode != null) ViewTailMode(this, new ViewTailModeRequestEventArgs(!viewTailModeToolStripButton.Checked)); });
		}

		public event TimelineControlDelegate Zoom;
		public event EventHandler ZoomToViewAll;
		public new event TimelineControlDelegate Scroll;
		public event ViewTailModeRequestDelegate ViewTailMode;

		public void SetHost(ITimelineControlPanelHost host)
		{
			this.host = host;
		}

		public void UpdateView()
		{
			viewTailModeToolStripButton.Checked = host.ViewTailMode;
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

		ITimelineControlPanelHost host;
	}

	public class TimelineControlEventArgs : EventArgs
	{
		public TimelineControlEventArgs(int delta)
		{
			this.delta = delta;
		}

		public int Delta { get { return delta; } }

		int delta;
	};

	public class ViewTailModeRequestEventArgs : EventArgs
	{
		public ViewTailModeRequestEventArgs(bool mode)
		{
			this.mode = mode;
		}

		public bool ViewTailModeRequested { get { return mode; } }

		bool mode;
	};

	public delegate void TimelineControlDelegate(object sender, TimelineControlEventArgs args);
	public delegate void ViewTailModeRequestDelegate(object sender, ViewTailModeRequestEventArgs args);
}
