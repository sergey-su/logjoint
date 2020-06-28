using System;
using System.Threading.Tasks;

namespace LogJoint
{
	class HeartBeatTimer : IHeartBeatTimer
	{
		public HeartBeatTimer()
		{
			this.worker = Worker();
		}

		public void SetTelemetryCollector(Telemetry.ITelemetryCollector collector)
		{
			this.collector = collector;
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

		private void TickHandler()
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
			/*for (; ; )
			{
				await Task.Delay(TimeSpan.FromMilliseconds(400)); // this works even if there is open modal dialog
				try
				{
					TickHandler();
				}
				catch (Exception e)
				{
					collector?.ReportException(e, "periodic timer");
				}
			}*/
		}

		int suspended = 0;
		int timerEventsCounter = 0;
		Task worker;
		Telemetry.ITelemetryCollector collector;
	}
}
