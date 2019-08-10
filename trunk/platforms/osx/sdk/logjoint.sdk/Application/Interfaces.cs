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
			Reactive.INSOutlineViewController CreateOutlineViewController(NSOutlineView outlineView);
			Reactive.INSTableViewController CreateTableViewController(NSTableView tableView);
		};
	}

	namespace UI.Reactive
	{
		public interface INSOutlineViewController
		{
			void Update(PR.ITreeNode newRoot);
			void ScrollToVisible(PR.ITreeNode item);
			Action<PR.ITreeNode> OnExpand { get; set; }
			Action<PR.ITreeNode> OnCollapse { get; set; }
			Action<PR.ITreeNode[]> OnSelect { get; set; }
			Func<NSTableColumn, PR.ITreeNode, NSView> OnView { get; set; }
		};

		public delegate NSView CrateTableViewDelegate(PR.IListItem item, NSTableColumn column);
		public delegate void UpdateTableViewDelegate(PR.IListItem item, NSTableColumn column, NSView view, PR.IListItem oldItem);

		public interface INSTableViewController
		{
			void Update(IReadOnlyList<PR.IListItem> newList);
			Action<PR.IListItem[]> OnSelect { get; set; }
			CrateTableViewDelegate OnCreateView { get; set; }
			UpdateTableViewDelegate OnUpdateView { get; set; }
		};
	}
}
