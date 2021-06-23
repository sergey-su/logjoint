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

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.timelineToolBox.SetPresenter(viewModel);

			viewModel.ChangeNotification.CreateSubscription(Updaters.Create(() => viewModel.IsEnabled, enabled =>
			{
				timelineToolBox.Enabled = enabled;
			}));
		}
	}
}
