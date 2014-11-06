using LogJoint.UI.Presenters;
using System;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class HeartBeatTimer : IHeartBeatTimer, Presenters.IViewUpdates
	{
		public HeartBeatTimer(Form mainForm)
		{
			this.mainForm = mainForm;
			this.timer = new System.Windows.Forms.Timer();
			this.timer.Enabled = true;
			this.timer.Interval = 400;
			this.timer.Tick += new System.EventHandler(this.timerTickHandler);
		}

		public event EventHandler<HeartBeatEventArgs> OnTimer;

		void IHeartBeatTimer.Suspend()
		{
			++suspended;
		}

		void IHeartBeatTimer.Resume()
		{
			if (suspended == 0)
				throw new InvalidOperationException("Can not resume not suspended timer");
			--suspended;
		}

		void IViewUpdates.RequestUpdate()
		{
			Tick(HeartBeatEventType.NormalUpdate);
		}

		void IViewUpdates.PostUpdateToUIDispatcherQueue()
		{
			Action action = () => Tick(HeartBeatEventType.NormalUpdate);
			mainForm.BeginInvoke(action);
		}


		private void timerTickHandler(object sender, EventArgs e)
		{
			if (suspended == 0)
			{
				++timerEventsCounter;

				HeartBeatEventType eventType = HeartBeatEventType.FrequentUpdate;
				if ((timerEventsCounter % 3) == 0)
					eventType |= HeartBeatEventType.NormalUpdate;
				if ((timerEventsCounter % 6) == 0)
					eventType |= HeartBeatEventType.RareUpdate;

				Tick(eventType);
			}
		}

		void Tick(HeartBeatEventType eventType)
		{
			if (OnTimer != null)
				OnTimer(this, new HeartBeatEventArgs(eventType));
		}

		readonly Form mainForm;
		readonly System.Windows.Forms.Timer timer;

		int suspended = 0;
		int timerEventsCounter = 0;
	}
}
