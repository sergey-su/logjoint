using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using AppKit;
using Foundation;
using LogJoint.Postprocessing;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class StateInspectorWindowController : 
		AppKit.NSWindowController,
		IView,
		Presenters.Postprocessing.IPostprocessorOutputForm
	{
		private readonly UI.Mac.IReactive reactive;
		private Reactive.INSOutlineViewController<IObjectsTreeNode> treeViewController;
		private Reactive.INSTableViewController<IStateHistoryItem> stateHistoryController;
		private readonly PropertiesViewDataSource propsDataSource = new PropertiesViewDataSource ();
		IViewModel viewModel;
		StateInspectorWindow windowRef;
		ContextMenuDelegate contextMenuDelegate;

		public StateInspectorWindowController (Mac.IReactive reactive) : base ("StateInspectorWindow")
		{
			this.reactive = reactive;
		}

		internal IViewModel ViewModel { get { return viewModel; } }

		internal PropertiesViewDataSource PropsDataSource { get { return propsDataSource; } }

		internal NSTableColumn PropKeyColumn { get { return propKeyColumn; } }

		internal NSTableColumn PropValueColumn { get { return propValueColumn; } }

		internal NSOutlineView TreeView { get { return treeView; } }

		internal new StateInspectorWindow Window { get { return (StateInspectorWindow)base.Window; } }

		internal NSTableView HistoryTableView { get { return stateHistoryView; } }

		internal NSTableColumn HistoryItemTextColumn { get { return historyItemTextColumn; } }

		internal NSTableColumn HistoryItemDecorationColumn { get { return historyItemDecorationColumn; } }

		internal NSTableColumn HistoryItemTimeColumn { get { return historyItemTimeColumn; } }

		internal void OnStateHistoryKeyEvent (Key key)
		{
			var sel = viewModel.ChangeHistoryItems.ElementAtOrDefault((int)stateHistoryView.SelectedRow);
			if (sel != null)
			{
				viewModel.OnChangeHistoryItemKeyEvent(sel, key);
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			windowRef = Window;
			windowRef.WillClose += (sender, e) => viewModel.OnVisibleChanged(false);
			windowRef.owner = this;

			treeViewController = reactive.CreateOutlineViewController<IObjectsTreeNode> (treeView);
			stateHistoryController = reactive.CreateTableViewController<IStateHistoryItem> (stateHistoryView);

			propertiesView.Init (this);
			propertiesView.Delegate = new PropertiesViewDelegate { owner = this, table = propertiesView };
			propertiesView.DataSource = propsDataSource;

			stateHistoryView.DoubleClick += (sender, e) => viewModel.OnChangeHistoryItemDoubleClicked(
				viewModel.ChangeHistoryItems[(int)stateHistoryView.ClickedRow]);
			((StateHistoryTableView)stateHistoryView).owner = this;

			findCurrentPositionInStateHistoryButton.Image.Template = true;
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			this.Window.EnsureCreated ();

			treeViewController.OnExpand = viewModel.OnExpandNode;
			treeViewController.OnCollapse = viewModel.OnCollapseNode;
			treeViewController.OnSelect = viewModel.OnSelect;
			treeViewController.OnView = (col, node) => {
				TreeNodeView view = (TreeNodeView)treeView.MakeView ("view", this);
				if (view == null)
					view = new TreeNodeView {
						owner = this,
						Identifier = "view",
						Menu = new NSMenu { Delegate = GetContextMenuDelegate () }
					};
				view.Update (node);
				return view;
			};
			treeViewController.OnRow = (node) => {
				TreeRowView view = (TreeRowView)treeView.MakeView ("row", this);
				if (view == null)
					view = new TreeRowView {
						owner = this,
						Identifier = "row"
					};
				view.Update (node);
				return view;
			};
			treeViewController.OnUpdateRow = (rowView, node) => {
				((TreeRowView)rowView).Update (node);
			};


			stateHistoryController.OnSelect = items => viewModel.OnChangeHistoryChangeSelection (items);
			stateHistoryController.OnCreateView = (historyItem, tableColumn) => {

				NSTextField MakeTextField (string viewId)
				{
					NSTextField view = (NSTextField)stateHistoryView.MakeView (viewId, this);
					if (view == null) {
						view = new NSTextField () {
							Identifier = viewId,
							Editable = false,
							Selectable = false,
							Bordered = false,
							BackgroundColor = NSColor.Clear
						};
						view.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
					}
					return view;
				}

				if (tableColumn == HistoryItemTimeColumn) {
					var view = MakeTextField ("timeView");
					view.StringValue = historyItem.Time;
					return view;
				} else if (tableColumn == HistoryItemTextColumn) {
					var view = MakeTextField ("messageView");
					view.StringValue = historyItem.Message;
					return view;
				}

				return null;
			};
			stateHistoryController.OnUpdateView = (item, tableColumn, view, oldItem) => {
				if (tableColumn == HistoryItemTimeColumn) {
					((NSTextField)view).StringValue = item.Time;
				} else if (tableColumn == HistoryItemTextColumn) {
					((NSTextField)view).StringValue = item.Message;
				}
			};

			stateHistoryController.OnCreateRowView = (item, rowIndex) =>
			{
				return new StateHistoryTableRowView { owner = this, row = rowIndex, items = viewModel.ChangeHistoryItems };
			};


			var updateTree = Updaters.Create (
				() => viewModel.ObjectsTreeRoot,
				treeViewController.Update
			);

			var invalidateTree = Updaters.Create (
				() => viewModel.PaintNode,
				() => viewModel.ColorTheme,
				(_1, _2) => InvalidateTree()
			);

			var updateStateHistory = Updaters.Create (
				() => viewModel.ChangeHistoryItems,
				items =>
				{
					stateHistoryController.Update (items);
					UpdateStateHistoryTimeColumn (items);
				}
			);

			var invalidateStateHistory = Updaters.Create (
				() => viewModel.IsChangeHistoryItemBookmarked,
				() => viewModel.FocusedMessagePositionInChangeHistory,
				(_1, _2) => InvalidateStateHistoryTableView ()
			);

			var updateProperties = Updaters.Create (
				() => viewModel.ObjectsProperties,
				properties => {
					propsDataSource.data = properties;
					propertiesView.ReloadData ();
				}
			);

			var updateCurrentTime = Updaters.Create (
				() => viewModel.CurrentTimeLabelText,
				timeValue => currentTimeLabel.StringValue = timeValue
			);

			viewModel.ChangeNotification.CreateSubscription (() => {
				updateTree ();
				invalidateTree ();
				updateStateHistory ();
				invalidateStateHistory ();
				updateProperties ();
				updateCurrentTime ();
			});
		}

		void IView.Show ()
		{
			ShowInternal ();
		}

		void IView.ScrollStateHistoryItemIntoView (int itemIndex)
		{
			var items = viewModel.ChangeHistoryItems;
			if (items.Count == 0)
				return;
			stateHistoryView.ScrollRowToVisible (RangeUtils.PutInRange (0, items.Count - 1, itemIndex));
		}

		void InvalidateTree ()
		{
			treeView.NeedsDisplay = true;
			var rows = treeView.RowsInRect (treeView.VisibleRect());
			for (var r = 0; r < rows.Length; ++r) {
				var rv = treeView.GetRowView (r + rows.Location, false);
				if (rv != null)
					rv.NeedsDisplay = true;
				var nv = treeView.GetView(0, r + rows.Location, false);
				if (nv != null)
					nv.NeedsDisplay = true;
			}
		}

		void UpdateStateHistoryTimeColumn (IReadOnlyList<IStateHistoryItem> items)
		{
			var widestCell = items.Select (
				(item, idx) => (item, idx)
			).MaxByKey(
				i => i.item.Time.Length
			);
			if (widestCell.item != null) {
				var cellView = (NSTextField)stateHistoryView.GetView (1, widestCell.idx, makeIfNecessary: true);
				cellView.SizeToFit ();
				historyItemTimeColumn.Width = cellView.Frame.Width + 10;
			} else {
				historyItemTimeColumn.Width = 0;
			}
		}

		void Presenters.Postprocessing.IPostprocessorOutputForm.Show ()
		{
			ShowInternal ();
		}

		internal void OnCancelOperation()
		{
			windowRef.Close ();
		}

		void ShowInternal ()
		{
			var w = Window;
			var wasVisible = w.IsVisible;
			w.MakeKeyAndOrderFront (null);
			if (!wasVisible)
				viewModel.OnVisibleChanged (true);
		}

		void InvalidateStateHistoryTableView()
		{
			for (int ridx = 0; ridx < stateHistoryView.RowCount; ++ridx)
			{
				var v = stateHistoryView.GetRowView(ridx, false);
				if (v != null)
					v.NeedsDisplay = true;
			}
		}

		partial void OnFindCurrentPositionInStateHistory (NSObject sender)
		{
			viewModel.OnFindCurrentPositionInChangeHistory();
		}
		
		internal ContextMenuDelegate GetContextMenuDelegate()
		{
			if (contextMenuDelegate == null)
				contextMenuDelegate = new ContextMenuDelegate() { eventsHandler = viewModel };
			return contextMenuDelegate;
		}
	}

	class ContextMenuDelegate : NSMenuDelegate
	{
		public IViewModel eventsHandler;

		public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
		{
		}
		
		public override void MenuWillOpen (NSMenu menu)
		{
			menu.RemoveAllItems();
			var menuData = eventsHandler.OnNodeMenuOpening();
			if (menuData.Items != null && menuData.Items.Count > 0)
			{
				foreach (var extItem in menuData.Items)
					menu.AddItem(new NSMenuItem(extItem.Text, (sender, e) => extItem.Click()));
			}
		}
	};

	[Register ("StateHistoryTableView")]
	class StateHistoryTableView: NSTableView
	{
		internal StateInspectorWindowController owner;

		public StateHistoryTableView (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public StateHistoryTableView (NSCoder coder) : base (coder)
		{
		}

		public override void KeyDown (NSEvent theEvent)
		{
			if (owner != null)
			{
				var chars = (theEvent.Characters ?? "").ToLower();
				if (chars == "b")
				{
					owner.OnStateHistoryKeyEvent(Key.BookmarkShortcut);
					return;
				}
				else if (chars == "\r" || chars == "\n")
				{
					owner.OnStateHistoryKeyEvent(Key.Enter);
					return;
				}
			}
			base.KeyDown (theEvent);
		}
	}
}
