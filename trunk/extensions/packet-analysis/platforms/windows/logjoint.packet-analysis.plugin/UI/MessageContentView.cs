using System.Linq;
using System.Windows.Forms;
using LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.PacketAnalysis.UI
{
	public partial class MessageContentView : UserControl, IView
	{
		readonly LogJoint.UI.Windows.Reactive.ITreeViewController treeViewController;

		public MessageContentView(LogJoint.UI.Windows.IView appViewLayer)
		{
			InitializeComponent();
			treeViewController = appViewLayer.Reactive.CreateTreeViewController(treeView);
		}

		object IView.OSView => this;

		void IView.SetViewModel(IViewModel viewModel)
		{
			var updatesTree = Updaters.Create(() => viewModel.Root, treeViewController.Update);

			viewModel.ChangeNotification.CreateSubscription(updatesTree);

			treeViewController.OnCollapse = n => viewModel.OnCollapse((IViewTreeNode)n);
			treeViewController.OnExpand = n => viewModel.OnExpand((IViewTreeNode)n);
			treeViewController.OnSelect = n => viewModel.OnSelect(n.OfType<IViewTreeNode>().FirstOrDefault());
		}
	}
}
