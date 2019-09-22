using System;
using WF = System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;
using System.Collections.Generic;

namespace LogJoint.UI.Windows.Reactive
{
	public interface ITreeViewController<Node> where Node: class, ITreeNode
	{
		void Update(Node newRoot);
		Action<Node[]> OnSelect { get; set; }
		Action<Node> OnExpand { get; set; }
		Action<Node> OnCollapse { get; set; }
		/// <summary>
		/// a hook that is called when the controller needs to update TreeNode object
		/// to represent a ITreeNode. Old ITreeNode or null is passed as 3rd argument.
		/// </summary>
		Action<WF.TreeNode, Node, Node> OnUpdateNode { get; set; }
		Node Map(WF.TreeNode node);
		WF.TreeNode Map(Node node);
	}

	public interface IListBoxController<Item> where Item: IListItem
	{
		void Update(IReadOnlyList<Item> newRoot);
		bool IsUpdating { get; }
		Action<Item[]> OnSelect { get; set; }
		Action<Item, int, Item> OnUpdateRow { get; set; }
		Item Map(object listBoxItem);
	};

	public interface IReactive
	{
		ITreeViewController<Node> CreateTreeViewController<Node>(MultiselectTreeView treeView) where Node : class, ITreeNode;
		IListBoxController<Item> CreateListBoxController<Item>(WF.ListBox listBox) where Item : class, IListItem;
	};
}
