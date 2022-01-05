namespace LogJoint.UI.Presenters.LogViewer
{
	public class ScreenBufferFactory : IScreenBufferFactory
	{
		readonly IChangeNotification changeNotification;
		readonly IBookmarksFactory bookmarksFactory;

		public ScreenBufferFactory(IChangeNotification changeNotification, IBookmarksFactory bookmarksFactory)
		{
			this.changeNotification = changeNotification;
			this.bookmarksFactory = bookmarksFactory;
		}

		IScreenBuffer IScreenBufferFactory.CreateScreenBuffer(double viewSize, LJTraceSource trace)
		{
			return new ScreenBuffer(changeNotification, bookmarksFactory, viewSize, trace);
		}
	};
};