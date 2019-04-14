namespace LogJoint.UI.Presenters.LogViewer
{
	public class ScreenBufferFactory : IScreenBufferFactory
	{
		readonly IChangeNotification changeNotification;

		public ScreenBufferFactory(IChangeNotification changeNotification)
		{
			this.changeNotification = changeNotification;
		}

		IScreenBuffer IScreenBufferFactory.CreateScreenBuffer(double viewSize, LJTraceSource trace)
		{
			return new ScreenBuffer(changeNotification, viewSize, trace);
		}
	};
};