using LogJoint.UI.Presenters;
using System;
using MonoMac.Foundation;

namespace LogJoint.UI
{
	class HeartBeatTimer : NSObject, IHeartBeatTimer, Presenters.IViewUpdates // todo: refactor to share with win
	{
		public HeartBeatTimer()
		{
			NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(400), () => timerTickHandler(null, null));
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
			InvokeOnMainThread (() => action());
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

		int suspended = 0;
		int timerEventsCounter = 0;
	}
}
