using System;
using WF = System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;

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
	}

	public interface IReactive
	{
		ITreeViewController CreateTreeViewController(WF.TreeView treeView);
	};
}
