using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using LogJoint.UI;
using AppKit;
using CoreText;
using Foundation;
using ObjCRuntime;
using System.Threading.Tasks;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class StateInspectorWindowController : 
		AppKit.NSWindowController,
		IView,
		Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm
	{
		IViewEvents eventsHandler;
		readonly TreeDataSource treeDataSource = new TreeDataSource ();
		StateInspectorWindow windowRef;
		bool updatingTree;
		readonly List<NodeOp> delayedTreeOps = new List<NodeOp>();
		readonly PropertiesViewDataSource propsDataSource = new PropertiesViewDataSource ();
		readonly StateHistoryDataSource stateHistoryDataSource = new StateHistoryDataSource();
		bool updatingStateHistory;
		ContextMenuDelegate contextMenuDelegate;

		#region Constructors

		// Called when created from unmanaged code
		public StateInspectorWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public StateInspectorWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public StateInspectorWindowController () : base ("StateInspectorWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		internal IViewEvents EventsHandler { get { return eventsHandler; } }

		internal PropertiesViewDataSource PropsDataSource { get { return propsDataSource; } }

		internal NSTableColumn PropKeyColumn { get { return propKeyColumn; } }

		internal NSTableColumn PropValueColumn { get { return propValueColumn; } }

		internal NSOutlineView TreeView { get { return treeView; } }

		internal StateHistoryDataSource StateHistoryDataSource { get { return stateHistoryDataSource; } }

		internal new StateInspectorWindow Window { get { return (StateInspectorWindow)base.Window; } }

		internal NSTableView HistoryTableView { get { return stateHistoryView; } }

		internal NSTableColumn HistoryItemTextColumn { get { return historyItemTextColumn; } }

		internal NSTableColumn HistoryItemDecorationColumn { get { return historyItemDecorationColumn; } }

		internal NSTableColumn HistoryItemTimeColumn { get { return historyItemTimeColumn; } }

		internal bool IsUpdatingStateHistory { get { return updatingStateHistory; }}

		internal void OnStateHistoryKeyEvent(Key key)
		{
			var sel = stateHistoryDataSource.data.ElementAtOrDefault((int)stateHistoryView.SelectedRow);
			if (sel != null)
			{
				eventsHandler.OnChangeHistoryItemKeyEvent(sel, key);
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			windowRef = Window;
			windowRef.WillClose += (sender, e) => eventsHandler.OnVisibleChanged();
			windowRef.owner = this;

			treeView.Delegate = new TreeViewDelegate () { owner = this };
			treeView.DataSource = treeDataSource;

			propertiesView.Init (this);
			propertiesView.Delegate = new PropertiesViewDelegate { owner = this, table = propertiesView };
			propertiesView.DataSource = propsDataSource;

			stateHistoryView.Delegate = new StateHistoryViewDelegate () { owner = this };
			stateHistoryView.DataSource = stateHistoryDataSource;
			stateHistoryView.DoubleClick += (sender, e) => eventsHandler.OnChangeHistoryItemClicked(
				item: stateHistoryDataSource.data.ElementAtOrDefault((int)stateHistoryView.ClickedRow));
			((StateHistoryTableView)stateHistoryView).owner = this;

			findCurrentPositionInStateHistoryButton.Image.Template = true;
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Clear (NodesCollectionInfo nodesCollection)
		{
			EnsureLoaded ();
			AssertUpdatingTree ();
			Node.FromNodesCollectionInfo (nodesCollection).children.Clear ();
		}

		void IView.AddNode (NodesCollectionInfo nodesCollection, NodeInfo node)
		{
			EnsureLoaded ();
			AssertUpdatingTree ();
			var parent = Node.FromNodesCollectionInfo (nodesCollection);
			var n = Node.FromNodeInfo (node);
			parent.children.Add (n);
			n.parent = parent;
		}

		NodeInfo IView.CreateNode (string nodeText, object tag, NodesCollectionInfo nodesCollection)
		{
			var newNode = new Node () 
			{
				tag = tag,
				text = nodeText
			};
			var parent = Node.FromNodesCollectionInfo (nodesCollection);
			if (parent != null) {
				EnsureLoaded ();
				AssertUpdatingTree ();
				parent.children.Add (newNode);
				newNode.parent = parent;
			}
			return newNode.ToNodeInfo ();
		}

		void IView.SetNodeText (NodeInfo node, string text)
		{
			EnsureLoaded ();
			var n = Node.FromNodeInfo (node);
			n.text = text;
			ExecuteNodeOp (n, NodeOpType.InvalidateNodeView);
		}

		void IView.BeginTreeUpdate ()
		{
			EnsureLoaded ();
			updatingTree = true;
			delayedTreeOps.Clear ();
		}

		void IView.EndTreeUpdate ()
		{
			EnsureLoaded ();
			AssertUpdatingTree ();
			updatingTree = false;
			treeView.ReloadData ();
			foreach (var op in delayedTreeOps)
				op.Execute (treeView, playingDelayedOps: true);
			delayedTreeOps.Clear ();
		}

		void IView.InvalidateTree ()
		{
			EnsureLoaded ();
			treeView.NeedsDisplay = true;
		}

		void IView.ExpandAll (NodeInfo node)
		{
			EnsureLoaded ();
			ExecuteNodeOp (Node.FromNodeInfo (node), NodeOpType.ExpandAll);
		}

		void IView.Collapse (NodeInfo node)
		{
			EnsureLoaded ();
			ExecuteNodeOp (Node.FromNodeInfo (node), NodeOpType.Collapse);
		}

		IEnumerable<NodeInfo> IView.EnumCollection (NodesCollectionInfo nodesCollection)
		{
			EnsureLoaded ();
			return Node.FromNodesCollectionInfo(nodesCollection).children.Select (n => n.ToNodeInfo ());
		}

		void IView.SetNodeColoring (NodeInfo node, NodeColoring coloring)
		{
			EnsureLoaded ();
			var n = Node.FromNodeInfo (node);
			n.coloring = coloring;
			ExecuteNodeOp (n, NodeOpType.InvalidateNodeView);
		}

		void IView.ScrollSelectedNodesInView ()
		{
			EnsureLoaded ();
			var selectedRow = treeView.SelectedRow;
			if (selectedRow < 0)
				return;
			var rect = treeView.FrameOfOutlineCellAtRow (selectedRow);
			treeView.ScrollRectToVisible (rect);
		}

		void IView.BeginUpdateStateHistoryList (bool fullUpdate, bool clearList)
		{
			EnsureLoaded ();

			if (clearList)
				stateHistoryDataSource.data.Clear ();
			updatingStateHistory = true;
		}

		int IView.AddStateHistoryItem (StateHistoryItem item)
		{
			EnsureLoaded ();
			if (!updatingStateHistory)
				throw new InvalidOperationException ();
			stateHistoryDataSource.data.Add (item);
			return stateHistoryDataSource.data.Count - 1;
		}

		void IView.EndUpdateStateHistoryList (int[] newSelectedIndexes, bool fullUpdate, bool redrawFocusedMessageMark)
		{
			EnsureLoaded ();
			if (!updatingStateHistory)
				throw new InvalidOperationException ();
			if (fullUpdate)
				stateHistoryView.ReloadData();
			if (newSelectedIndexes != null)
				stateHistoryView.SelectRows(
					NSIndexSet.FromArray(newSelectedIndexes),
					byExtendingSelection: false
				);
			if (redrawFocusedMessageMark)
				InvalidateStateHistoryTableView();
			if (fullUpdate)
				UpdateStateHistoryTimeColumn();
			updatingStateHistory = false;
		}

		void UpdateStateHistoryTimeColumn()
		{
			float w = 0;
			for (int i = 0; i < stateHistoryDataSource.data.Count; ++i)
			{
				var cellView = (NSTextField)stateHistoryView.GetView(1, i, makeIfNecessary: true);
				cellView.SizeToFit();
				w = Math.Max(w, (float)cellView.Frame.Width);
			}
			historyItemTimeColumn.Width = w;
		}

		IEnumerable<StateHistoryItem> IView.SelectedStateHistoryEvents {
			get {
				if (windowRef == null)
					Enumerable.Empty<StateHistoryItem>();
				return
					stateHistoryDataSource.data
					.ZipWithIndex()
					.Where(i => stateHistoryView.IsRowSelected(i.Key))
					.Select(i => i.Value);
			}
		}

		void IView.ScrollStateHistoryItemIntoView(int itemIndex)
		{
			if (stateHistoryDataSource.data.Count == 0)
				return;
			stateHistoryView.ScrollRowToVisible(RangeUtils.PutInRange(0, stateHistoryDataSource.data.Count - 1, itemIndex));
		}

		void IView.SetPropertiesDataSource (IList<KeyValuePair<string, object>> properties)
		{
			EnsureLoaded ();

			propsDataSource.data = properties;
			propertiesView.ReloadData ();
		}

		void IView.SetCurrentTimeLabelText (string text)
		{
			EnsureLoaded ();

			currentTimeLabel.StringValue = text;
		}

		void IView.Show ()
		{
			ShowInternal ();
		}

		bool IView.Visible 
		{
			get { return windowRef != null && windowRef.IsVisible; }
		}

		NodesCollectionInfo IView.RootNodesCollection 
		{
			get { return treeDataSource.root.ToNodesCollection (); }
		}

		NodeInfo[] IView.SelectedNodes 
		{
			get
			{
				if (windowRef == null)
					return new NodeInfo[0];
				switch (treeView.SelectedRowCount) {
				case 0:
					return new NodeInfo[0];
				case 1:
					return new [] {((Node)treeView.ItemAtRow (treeView.SelectedRow)).ToNodeInfo ()};
				default:
					return 
						Enumerable.Range (0, (int)treeView.RowCount)
						.Where (r => treeView.IsRowSelected (r))
						.Select (r => treeView.ItemAtRow ((int)r))
						.OfType<Node> ()
						.Select (n => n.ToNodeInfo ())
						.ToArray ();
				}
			}
			set {
				EnsureLoaded ();
				UIUtils.SelectAndScrollInView(treeView, value.Select(Node.FromNodeInfo).ToArray(), i => i.parent);
			}
		}

		int? IView.SelectedPropertiesRow {
			get {
				var x = (int)propertiesView.SelectedRow;
				return x >= 0 ? x : new int?();
			}
			set {
				if (value != null)
					propertiesView.SelectRow (value.Value, false);
				else
					propertiesView.SelectRows (new NSIndexSet (), false);
			}
		}

		bool IView.TreeSupportsLoadingOnExpansion
		{
			get { return false; }
		}

		void Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm.Show ()
		{
			ShowInternal ();
		}

		internal void OnCancelOperation()
		{
			windowRef.Close ();
		}

		void AssertUpdatingTree()
		{
			if (!updatingTree)
				throw new InvalidOperationException ();
		}

		void ShowInternal ()
		{
			var w = Window;
			var wasVisible = w.IsVisible;
			w.MakeKeyAndOrderFront (null);
			if (!wasVisible)
				eventsHandler.OnVisibleChanged ();
		}

		void EnsureLoaded()
		{
			Window.GetHashCode (); // getting Window loads the nib
		}

		void ExecuteNodeOp(Node target, NodeOpType op)
		{
			var opObj = new NodeOp (op, target);
			if (updatingTree)
				delayedTreeOps.Add (opObj);
			else
				opObj.Execute (treeView, false);
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
			eventsHandler.OnFindCurrentPositionInStateHistory();
		}
		
		internal ContextMenuDelegate GetContextMenuDelegate()
		{
			if (contextMenuDelegate == null)
				contextMenuDelegate = new ContextMenuDelegate() { eventsHandler = eventsHandler };
			return contextMenuDelegate;
		}
	}

	class ContextMenuDelegate : NSMenuDelegate
	{
		public IViewEvents eventsHandler;

		public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
		{
		}
		
		public override void MenuWillOpen (NSMenu menu)
		{
			menu.RemoveAllItems();
			var menuData = eventsHandler.OnMenuOpening();
			if (menuData.Items != null && menuData.Items.Count > 0)
			{
				foreach (var extItem in menuData.Items)
					menu.AddItem(new NSMenuItem(extItem.Text, (sender, e) => extItem.Click()));
			}
		}
	};

	[Register ("StateHistoryTableView")]
	partial class StateHistoryTableView: NSTableView
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