using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AppKit;
using Foundation;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Reactive
{
	public class NSOutlineViewController<Node>: INSOutlineViewController<Node> where Node : class, ITreeNode
	{
		private readonly NSOutlineView treeView;
		private readonly Telemetry.ITelemetryCollector telemetryCollector;
		private readonly DataSource dataSource;
		private readonly Delegate @delegate;

		public NSOutlineViewController(NSOutlineView treeView, Telemetry.ITelemetryCollector telemetryCollector)
		{
			this.treeView = treeView;
			this.telemetryCollector = telemetryCollector;

			this.dataSource = new DataSource(this, treeView);
			this.@delegate = new Delegate(this);
			this.treeView.Delegate = @delegate;
			this.treeView.DataSource = dataSource;
		}

		public void Update(Node newRoot)
		{
			this.@delegate.updating = true;
			try
			{
				this.dataSource.Update(newRoot);
			}
			finally
			{
				this.@delegate.updating = false;
			}
		}

		public void ScrollToVisible (Node node)
		{
			if (dataSource.TryMapNodeToItem (node, out var item))
			{
				var row = treeView.RowForItem (item);
				if (row != -1)
					treeView.ScrollRowToVisible (row);
			}
		}

		public Action<Node> OnExpand { get; set; }
		public Action<Node> OnCollapse { get; set; }
		public Action<IReadOnlyList<Node>> OnSelect { get; set; }
		public Func<NSTableColumn, Node, NSView> OnView { get; set; }
		public Func<Node, NSTableRowView> OnRow { get; set; }
		public Action<NSTableRowView, Node> OnUpdateRow { get; set; }

		class DataSource : NSOutlineViewDataSource
		{
			readonly NSOutlineViewController<Node> owner;
			readonly NSOutlineView treeView;
			readonly NSNodeItem rootItem;
			readonly Dictionary<ITreeNode, NSNodeItem> nodeToItem;

			public DataSource(NSOutlineViewController<Node> owner, NSOutlineView treeView)
			{
				this.owner = owner;
				this.treeView = treeView;
				this.nodeToItem = new Dictionary<ITreeNode, NSNodeItem>();
				this.rootItem = CreateItem(EmptyTreeNode.Instance);
			}

			public bool TryMapNodeToItem (ITreeNode node, out NSNodeItem item)
			{
				return nodeToItem.TryGetValue (node, out item);
			}

			NSNodeItem CreateItem(ITreeNode node)
			{
				var result = new NSNodeItem { Node = node };
				nodeToItem.Add(node, result);
				return result;
			}

			public void Update(ITreeNode newRoot)
			{
				this.treeView.BeginUpdates();
				try
				{
					UpdateStructure(newRoot);
				}
				finally
				{
					this.treeView.EndUpdates();
				}
				UpdateSelection();
			}

			void UpdateStructure(ITreeNode newRoot)
			{
				void Rebind(NSNodeItem item, ITreeNode newNode)
				{
					nodeToItem.Remove(item.Node);
					item.Node = newNode;
					nodeToItem[newNode] = item;
				}

				void DeleteDescendantsFromMap(NSNodeItem item)
				{
					item.Children.ForEach(c =>
					{
						Debug.Assert(nodeToItem.Remove(c.Node));
						DeleteDescendantsFromMap(c);
					});
				}

				var edits = TreeEdit.GetTreeEdits(rootItem.Node, newRoot, new TreeEdit.Options
				{
					TemporariltyExpandParentToInitChildren = true
				});

				Rebind(rootItem, newRoot);

				foreach (var e in edits)
				{
					var node = nodeToItem[e.Node];
					switch (e.Type)
					{
					case TreeEdit.EditType.Insert:
						var insertedNode = CreateItem(e.NewChild);
						node.Children.Insert(e.ChildIndex, insertedNode);
						using (var set = new NSIndexSet (e.ChildIndex))
							treeView.InsertItems(set, ToObject(node), NSTableViewAnimation.None);
						break;
					case TreeEdit.EditType.Delete:
						var deletedNode = node.Children[e.ChildIndex];
						node.Children.RemoveAt(e.ChildIndex);
						Debug.Assert(deletedNode == nodeToItem[e.OldChild]);
						nodeToItem.Remove(e.OldChild);
						using (var set = new NSIndexSet (e.ChildIndex))
							treeView.RemoveItems (set, ToObject (node), NSTableViewAnimation.None);
						DeleteDescendantsFromMap(deletedNode);
						break;
					case TreeEdit.EditType.Reuse:
						var viewItem = nodeToItem [e.OldChild];
						Rebind (viewItem, e.NewChild);
						treeView.ReloadItem (ToObject (viewItem), reloadChildren: false);
						if (owner.OnUpdateRow != null) {
							var rowIdx = treeView.RowForItem (ToObject (viewItem));
							var rowView = rowIdx >= 0 ? treeView.GetRowView (rowIdx, makeIfNecessary: false) : null;
							if (rowView != null) {
								owner.OnUpdateRow (rowView, (Node)e.NewChild);
							}
						}
						break;
					case TreeEdit.EditType.Expand:
						treeView.ExpandItem(ToObject(node), expandChildren: false);
						break;
					case TreeEdit.EditType.Collapse:
						treeView.CollapseItem(ToObject(node), collapseChildren: false);
						break;
					}
				}
			}

			void UpdateSelection()
			{
				var rowsToSelect = new HashSet<nuint>();

				void DiscoverSelected(ITreeNode node) // todo: remove linear traversal
				{
					if (node.IsSelected)
					{
						var rowToSelect = treeView.RowForItem(ToObject(nodeToItem[node]));
						Debug.Assert(rowToSelect >= 0);
						rowsToSelect.Add((nuint)rowToSelect);
					}
					if (node.IsExpanded)
						foreach (var n in node.Children)
							DiscoverSelected(n);
				}

				DiscoverSelected(rootItem.Node);

				treeView.SelectRows(NSIndexSet.FromArray(rowsToSelect.ToArray()), byExtendingSelection: false);
			}

			public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
			{
				return ToItem(item).Children.Count;
			}

			public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				return ToItem(item).Children[(int)childIndex];
			}

			public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
			{
				return item == null || ToItem(item).Node.Children.Count > 0;
			}

			NSNodeItem ToItem(NSObject item) => item == null ? rootItem : (NSNodeItem)item;

			NSObject ToObject(NSNodeItem node) => node == rootItem ? null : node;
		};

		class Delegate : NSOutlineViewDelegate
		{
			private readonly NSOutlineViewController<Node> owner;
			public bool updating;
			Task notificationChain = Task.CompletedTask;

			public Delegate(NSOutlineViewController<Node> owner)
			{
				this.owner = owner;
			}

			public override NSView GetView(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
			{
				if (owner.OnView != null)
				{
					return owner.OnView(tableColumn, ((NSNodeItem)item).ModelNode);
				}
				var view = (NSTextField)outlineView.MakeView("defaultView", this);
				if (view == null)
					view = NSTextField.CreateLabel(item.ToString());
				else
					view.StringValue = item.ToString();
				return view;
			}

			[Export ("outlineView:rowViewForItem:")]
			public override NSTableRowView RowViewForItem (NSOutlineView outlineView, NSObject item)
			{
				if (owner.OnRow != null)
				{
					return owner.OnRow (((NSNodeItem)item).ModelNode);
				}
				return null;
			}

			public override NSIndexSet GetSelectionIndexes(NSOutlineView outlineView, NSIndexSet proposedSelectionIndexes)
			{
				if (updating)
					return proposedSelectionIndexes;
				Notify(() =>
				{
					owner.OnSelect?.Invoke(
						proposedSelectionIndexes
						.Select(row => outlineView.ItemAtRow((nint)row))
						.OfType<NSNodeItem>()
						.Select(n => n.ModelNode)
						.ToArray()
					);
				});
				return proposedSelectionIndexes;
			}

			public override bool ShouldExpandItem(NSOutlineView outlineView, NSObject item)
			{
				if (updating)
					return true;
				Notify(() =>
				{
					owner.OnExpand?.Invoke(((NSNodeItem)item).ModelNode);
				});
				return false;
			}

			public override bool ShouldCollapseItem(NSOutlineView outlineView, NSObject item)
			{
				if (updating)
					return true;
				Notify(() =>
				{
					owner.OnCollapse?.Invoke(((NSNodeItem)item).ModelNode);
				});
				return false;
			}

			void Notify(Action action)
			{
				notificationChain = notificationChain.ContinueWith(_ =>
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						owner.telemetryCollector.ReportException (e, "NSOutlineViewController notification");
					}
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
		};

		class NSNodeItem : NSObject
		{
			public ITreeNode Node;
			public readonly List<NSNodeItem> Children = new List<NSNodeItem>();
			public Node ModelNode => (Node)Node;

			public override string ToString() => Node.ToString();
		};
	}
}
