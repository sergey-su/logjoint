using System;
using System.Linq;
using System.Collections.Generic;
using AppKit;
using Foundation;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Reactive
{
	public class NSTableViewController<Item>: INSTableViewController<Item> where Item : class, IListItem
	{
		private readonly NSTableView tableView;
		private readonly Telemetry.ITelemetryCollector telemetryCollector;
		private readonly DataSource dataSource;
		private readonly Delegate @delegate;

		public NSTableViewController(NSTableView tableView, Telemetry.ITelemetryCollector telemetryCollector)
		{
			this.tableView = tableView;
			this.telemetryCollector = telemetryCollector;

			this.dataSource = new DataSource(this, tableView);
			this.@delegate = new Delegate(this, this.dataSource);
			this.tableView.Delegate = @delegate;
			this.tableView.DataSource = dataSource;
		}

		public void Update(IReadOnlyList<Item> newList)
		{
			this.@delegate.updating = true;
			try
			{
				this.dataSource.Update(newList);
			}
			finally
			{
				this.@delegate.updating = false;
			}
		}

		public Action<IReadOnlyList<Item>> OnSelect { get; set; }
		public CrateTableViewDelegate<Item> OnCreateView { get; set; }
		public UpdateTableViewDelegate<Item> OnUpdateView { get; set; }
		public CreateTableRowViewDelegate<Item> OnCreateRowView { get; set; }

		class DataSource : NSTableViewDataSource
		{
			readonly NSTableView tableView;
			readonly NSTableViewController<Item> owner;
			public IReadOnlyList<Item> items = new List<Item> ().AsReadOnly();

			public DataSource(NSTableViewController<Item> owner, NSTableView tableView)
			{
				this.tableView = tableView;
				this.owner = owner;
			}

			public override nint GetRowCount(NSTableView tableView) => items.Count;

			public void Update(IReadOnlyList<Item> newList)
			{
				this.tableView.BeginUpdates();
				try
				{
					UpdateStructure(newList);
				}
				finally
				{
					this.tableView.EndUpdates();
				}
				UpdateSelection();
			}

			void UpdateStructure(IReadOnlyList<Item> newList)
			{
				var edits = ListEdit.GetListEdits(items, newList);

				items = newList;

				var columns = tableView.TableColumns();
				NSIndexSet allColumnsSet = null;

				foreach (var e in edits)
				{
					switch (e.Type)
					{
					case ListEdit.EditType.Insert:
						using (var set = new NSIndexSet (e.Index))
							tableView.InsertRows(set, NSTableViewAnimation.None);
						break;
					case ListEdit.EditType.Delete:
						using (var set = new NSIndexSet (e.Index))
							tableView.RemoveRows (set, NSTableViewAnimation.None);
						break;
					case ListEdit.EditType.Reuse:
						if (owner.OnUpdateView != null && owner.OnCreateView != null)
						{
							for (int col = 0; col < columns.Length; ++col)
							{
								var existingView = tableView.GetView(col, e.Index, makeIfNecessary: false);
								if (existingView != null)
									owner.OnUpdateView((Item)e.Item, columns[col], existingView, (Item)e.OldItem);
							}
						}
						else
						{
							if (allColumnsSet == null)
								allColumnsSet = NSIndexSet.FromArray(Enumerable.Range(0, columns.Length).ToArray());
							using (var set = new NSIndexSet (e.Index))
								tableView.ReloadData (set, allColumnsSet);
						}
						break;
					}
				}

				allColumnsSet?.Dispose ();
			}

			void UpdateSelection()
			{
				var rowsToSelect = new HashSet<nuint>();

				nuint idx = 0;
				foreach (var i in items)
				{
					if (i.IsSelected)
						rowsToSelect.Add(idx);
					++idx;
				}

				if (rowsToSelect.SetEquals(tableView.SelectedRows))
					return;

				tableView.SelectRows(NSIndexSet.FromArray(rowsToSelect.ToArray()), byExtendingSelection: false);
			}
		};

		class Delegate : NSTableViewDelegate
		{
			private readonly NSTableViewController<Item> owner;
			private readonly DataSource dataSource;
			public bool updating;
			Task notificationChain = Task.CompletedTask;

			public Delegate(NSTableViewController<Item> owner, DataSource dataSource)
			{
				this.owner = owner;
				this.dataSource = dataSource;
			}

			public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var item = dataSource.items[(int)row];
				if (owner.OnCreateView != null)
				{
					return owner.OnCreateView(item, tableColumn);
				}
				var view = (NSTextField)tableView.MakeView("defaultView", this);
				if (view == null)
					view = NSTextField.CreateLabel(item.ToString());
				else
					view.StringValue = item.ToString();
				return view;
			}

			public override NSTableRowView CoreGetRowView (NSTableView tableView, nint row)
			{
				if (owner.OnCreateRowView != null)
				{
					var item = dataSource.items [(int)row];
					return owner.OnCreateRowView (item, (int)row);
				}
				return null;
			}

			public override NSIndexSet GetSelectionIndexes(NSTableView tableView, NSIndexSet proposedSelectionIndexes)
			{
				if (updating)
					return proposedSelectionIndexes;
				Notify(() =>
				{
					owner.OnSelect?.Invoke(
						proposedSelectionIndexes
						.Select(row => dataSource.items[(int)row])
						.ToArray()
					);
				});
				return proposedSelectionIndexes;
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
						owner.telemetryCollector.ReportException (e, "NSTableViewController notification");
					}
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
		};
	}
}
