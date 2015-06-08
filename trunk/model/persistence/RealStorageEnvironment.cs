using System;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class RealEnvironment : IEnvironment
	{
		public DateTime Now
		{
			get { return DateTime.Now; }
		}
		public TimeSpan MinimumTimeBetweenCleanups
		{
			get { return TimeSpan.FromHours(24 * 3); } // todo: hardcoded value 
		}
		public long MaximumStorageSize
		{
			get { return 16 * 1024 * 1024; } // todo: get rid of hardcoded value
		}
		public Task StartCleanupWorker(Action cleanupRoutine)
		{
			var t = new Task(cleanupRoutine);
			t.Start();
			return t;
		}

		public Settings.IGlobalSettingsAccessor CreateSettingsAccessor(IStorageManager storageManager)
		{
			return new Settings.GlobalSettingsAccessor(storageManager.GlobalSettingsEntry);
		}
	};
}
