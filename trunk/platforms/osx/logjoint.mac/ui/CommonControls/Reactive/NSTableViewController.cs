using System;
using System.Linq;
using System.Collections.Generic;
using AppKit;
using Foundation;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Reactive
{
	public class NSTableViewController: INSTableViewController
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

		public void Update(IReadOnlyList<IListItem> newList)
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

		public Action<IListItem[]> OnSelect { get; set; }
		public CrateTableViewDelegate OnCreateView { get; set; }
		public UpdateTableViewDelegate OnUpdateView { get; set; }
		public CreateTableRowViewDelegate OnCreateRowView { get; set; }

		class DataSource : NSTableViewDataSource
		{
			readonly NSTableView tableView;
			readonly NSTableViewController owner;
			public IReadOnlyList<IListItem> items = new List<IListItem>().AsReadOnly();

			public DataSource(NSTableViewController owner, NSTableView tableView)
			{
				this.tableView = tableView;
				this.owner = owner;
			}

			public override nint GetRowCount(NSTableView tableView) => items.Count;

			public void Update(IReadOnlyList<IListItem> newList)
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

			void UpdateStructure(IReadOnlyList<IListItem> newList)
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
						tableView.InsertRows(new NSIndexSet(e.Index), NSTableViewAnimation.None);
						break;
					case ListEdit.EditType.Delete:
						tableView.RemoveRows(new NSIndexSet(e.Index), NSTableViewAnimation.None);
						break;
					case ListEdit.EditType.Reuse:
						if (owner.OnUpdateView != null && owner.OnCreateView != null)
						{
							for (int col = 0; col < columns.Length; ++col)
							{
								var existingView = tableView.GetView(col, e.Index, makeIfNecessary: false);
								if (existingView != null)
									owner.OnUpdateView(e.Item, columns[col], existingView, e.OldItem);
							}
						}
						else
						{
							if (allColumnsSet == null)
								allColumnsSet = NSIndexSet.FromArray(Enumerable.Range(0, columns.Length).ToArray());
							tableView.ReloadData(new NSIndexSet(e.Index), allColumnsSet);
						}
						break;
					}
				}
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
			private readonly NSTableViewController owner;
			private readonly DataSource dataSource;
			public bool updating;
			Task notificationChain = Task.CompletedTask;

			public Delegate(NSTableViewController owner, DataSource dataSource)
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
				return base.CoreGetRowView(tableView, row);
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
