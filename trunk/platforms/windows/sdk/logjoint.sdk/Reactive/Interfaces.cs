using System;
using WF = System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;
using System.Collections.Generic;

namespace LogJoint.UI.Windows.Reactive
{
	public interface ITreeViewController
	{
		void Update(ITreeNode newRoot);
		Action<ITreeNode[]> OnSelect { get; set; }
		Action<ITreeNode> OnExpand { get; set; }
		Action<ITreeNode> OnCollapse { get; set; }
		/// <summary>
		/// a hook that is called when the controller needs to update TreeNode object
		/// to represent a ITreeNode. Old ITreeNode or null is passed as 3rd argument.
		/// </summary>
		Action<WF.TreeNode, ITreeNode, ITreeNode> OnUpdateNode { get; set; }
		ITreeNode Map(WF.TreeNode node);
		WF.TreeNode Map(ITreeNode node);
	}

	public interface IListBoxController
	{
		void Update(IReadOnlyList<IListItem> newRoot);
		bool IsUpdating { get; }
		Action<IListItem[]> OnSelect { get; set; }
		Action<IListItem, int, IListItem> OnUpdateRow { get; set; }
	};

	public interface IReactive
	{
		ITreeViewController CreateTreeViewController(MultiselectTreeView treeView);
		IListBoxController CreateListBoxController(WF.ListBox listBox);
	};
}
