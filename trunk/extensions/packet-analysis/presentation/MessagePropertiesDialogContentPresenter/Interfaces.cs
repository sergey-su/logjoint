using System.Collections.Generic;
using LogJoint.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog
{
	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		object OSView { get; }
	};

	public interface IPresenter: IMessageContentPresenter
	{
		void SetMessage(IMessage message);
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		IViewTreeNode Root { get; }
		void OnExpand(IViewTreeNode node);
		void OnCollapse(IViewTreeNode node);
		void OnSelect(IViewTreeNode node);
	};

	public interface IViewTreeNode
	{
		string Text { get; }
		bool IsSelected { get; }
		IReadOnlyList<IViewTreeNode> Children { get; }
		bool IsExpanded { get; }
	};
};