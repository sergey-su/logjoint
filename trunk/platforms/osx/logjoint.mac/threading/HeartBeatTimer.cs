using LogJoint.UI.Presenters;
using System;
using System.Threading.Tasks;
using Foundation;

namespace LogJoint.UI
{
	class HeartBeatTimer : NSObject, IHeartBeatTimer, Presenters.IViewUpdates // todo: refactor to share with win
	{
		public HeartBeatTimer()
		{
			this.worker = Worker();
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
			BeginInvokeOnMainThread (() => action());
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
			OnTimer?.Invoke(this, new HeartBeatEventArgs(eventType));
		}

		async Task Worker()
		{
			for (;;)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(400)); // this works even if there is open modal dialog
				timerTickHandler(null, null);
			}
		}

		int suspended = 0;
		int timerEventsCounter = 0;
		Task worker;
	}
}
