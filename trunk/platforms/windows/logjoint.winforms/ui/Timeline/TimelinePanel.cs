using System.Windows.Forms;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelinePanel : UserControl, IView
	{
		public TimelinePanel()
		{
			InitializeComponent();
		}

		public TimeLineControl TimelineControl { get { return timeLineControl; } }

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
			this.timelineToolBox.SetPresenter(presenter);
		}

		void IView.SetEnabled(bool value)
		{
			timelineToolBox.Enabled = value;
		}

		IViewEvents presenter;
	}
}
