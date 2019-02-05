namespace LogJoint.UI.Presenters.LogViewer
{
	public class ScreenBufferFactory : IScreenBufferFactory
	{
		IScreenBuffer IScreenBufferFactory.CreateScreenBuffer(double viewSize, LJTraceSource trace)
		{
			return new ScreenBuffer(viewSize, trace);
		}
	};
};