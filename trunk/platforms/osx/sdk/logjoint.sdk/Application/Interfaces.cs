using System;
using System.Collections.Generic;
using AppKit;
using PR = LogJoint.UI.Presenters.Reactive;

namespace LogJoint
{
	public interface IApplication
	{
		IModel Model { get; }
		UI.Presenters.IPresentation Presentation { get; }
		UI.Mac.IView View { get; }
	};

	namespace UI.Mac
	{
		public interface IView
		{
			IReactive Reactive { get; }
		};

		public interface IReactive
		{
			Reactive.INSOutlineViewController<Node> CreateOutlineViewController<Node>(NSOutlineView outlineView) where Node : class, PR.ITreeNode;
			Reactive.INSTableViewController<Item> CreateTableViewController<Item>(NSTableView tableView) where Item : class, PR.IListItem;
		};
	}

	namespace UI.Reactive
	{
		public interface INSOutlineViewController<Node> where Node: class, PR.ITreeNode
		{
			void Update(Node newRoot);
			void ScrollToVisible(Node item);
			Action<Node> OnExpand { get; set; }
			Action<Node> OnCollapse { get; set; }
			Action<IReadOnlyList<Node>> OnSelect { get; set; }
			Func<NSTableColumn, Node, NSView> OnView { get; set; }
			Func<Node, NSTableRowView> OnRow { get; set; }
		};

		public delegate NSView CrateTableViewDelegate<Item>(Item item, NSTableColumn column);
		public delegate void UpdateTableViewDelegate<Item>(Item item, NSTableColumn column, NSView view, Item oldItem);
		public delegate NSTableRowView CreateTableRowViewDelegate<Item>(Item item, int rowIndex);

		public interface INSTableViewController<Item> where Item: class, PR.IListItem
		{
			void Update(IReadOnlyList<Item> newList);
			Action<IReadOnlyList<Item>> OnSelect { get; set; }
			CrateTableViewDelegate<Item> OnCreateView { get; set; }
			UpdateTableViewDelegate<Item> OnUpdateView { get; set; }
			CreateTableRowViewDelegate<Item> OnCreateRowView { get; set; }
		};
	}
}
