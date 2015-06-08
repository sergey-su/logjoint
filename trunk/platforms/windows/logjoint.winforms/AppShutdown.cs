using LogJoint.UI.Presenters.MainForm;

namespace LogJoint
{
	class AppShutdown: Shutdown
	{
		public void Attach(IPresenter mainForm)
		{
			mainForm.Closing += (s, e) => base.RunShutdownSequence();
		}
	}
}
