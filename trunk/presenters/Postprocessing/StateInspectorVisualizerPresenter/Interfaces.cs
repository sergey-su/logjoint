using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
	public interface IPresenter
	{
		bool IsObjectEventPresented(ILogSource source, TextLogEventTrigger eventTrigger);
		bool TrySelectObject(ILogSource source, TextLogEventTrigger objectEvent, Func<IInspectedObject, int> disambiguationFunction);
		void Show();
		IInspectedObject SelectedObject { get; }
		event EventHandler<MenuData> OnMenu;
		event EventHandler<NodeCreatedEventArgs> OnNodeCreated;
	};

	public interface IView
	{
		void SetEventsHandler(IViewModel eventsHandler);
		bool Visible { get; }
		NodesCollectionInfo RootNodesCollection { get; }
		void Clear(NodesCollectionInfo nodesCollection);
		void AddNode(NodesCollectionInfo nodesCollection, NodeInfo node);
		NodeInfo CreateNode(string nodeText, object tag, NodesCollectionInfo nodesCollection);
		void SetNodeText(NodeInfo node, string text);
		void BeginTreeUpdate();
		void EndTreeUpdate();
		void InvalidateTree();
		/// <summary>
		/// Expands all child nodes recursively. Is does not raise IViewEvents.OnNodeExpanding.
		/// </summary>
		void ExpandAll(NodeInfo node);
		void Collapse(NodeInfo node);
		IEnumerable<NodeInfo> EnumCollection(NodesCollectionInfo nodesCollection);
		NodeInfo[] SelectedNodes { get; set; }
		void SetNodeColoring(NodeInfo node, NodeColoring coloring);
		bool TreeSupportsLoadingOnExpansion { get; }
		void ScrollSelectedNodesInView();
		IEnumerable<StateHistoryItem> SelectedStateHistoryEvents { get; }
		void BeginUpdateStateHistoryList(bool fullUpdate, bool clearList);
		int AddStateHistoryItem(StateHistoryItem item);
		void EndUpdateStateHistoryList(int[] newSelectedIndexes, bool fullUpdate, bool redrawFocusedMessageMark);
		void ScrollStateHistoryItemIntoView(int itemIndex);

		int? SelectedPropertiesRow { get; set; }
		void SetPropertiesDataSource(IList<KeyValuePair<string, object>> properties);

		void SetCurrentTimeLabelText(string text);

		void Show();
	};

	public struct NodeInfo
	{
		public object Data;
		public object Tag;
		public NodesCollectionInfo ChildrenNodesCollection;
		public string Text;
		public NodeColoring Coloring;
	};

	public struct NodesCollectionInfo
	{
		public object Data;
	};

	[Flags]
	public enum NodeColoring
	{
		Alive = 1,
		Deleted = 2,
		NotCreatedYet = 3
	};

	public enum Key
	{
		None,
		Enter,
		BookmarkShortcut,
	};

	public interface IViewModel
	{
		ColorThemeMode ColorTheme { get; }
		void OnVisibleChanged();
		void OnSelectedNodesChanged();
		void OnPropertiesRowDoubleClicked();
		PropertyCellPaintInfo OnPropertyCellPaint(int rowIndex);
		void OnPropertyCellClicked(int rowIndex);
		void OnChangeHistoryItemClicked(StateHistoryItem item);
		void OnChangeHistoryItemKeyEvent(StateHistoryItem item, Key key);
		void OnChangeHistorySelectionChanged();
		NodePaintInfo OnPaintNode(NodeInfo node, bool getPrimaryPropValue);
		Tuple<int, int> OnDrawFocusedMessageMark();
		bool OnGetHistoryItemBookmarked(StateHistoryItem item);
		void OnCopyShortcutPressed();
		void OnDeleteKeyPressed();
		void OnNodeExpanding(NodeInfo info);
		void OnFindCurrentPositionInStateHistory();
		MenuData OnMenuOpening();
	};

	public struct PropertyCellPaintInfo
	{
		public bool PaintAsLink;
		public bool AddLeftPadding;
	};

	public struct NodePaintInfo
	{
		public bool DrawingEnabled;
		public NodeColoring Coloring;
		public string PrimaryPropValue;
		public bool DrawFocusedMsgMark;
	};

	public class StateHistoryItem
	{
		public string Time;
		public string Message;
		public object Data;
	};

	public class MenuData
	{
		public class Item
		{
			public string Text;
			public Action Click;
		};
		public List<Item> Items;
	};

	public class NodeCreatedEventArgs
	{
		public IInspectedObject NodeObject { get; internal set; }
		public bool? CreateCollapsed { get; set; }
		public bool? CreateLazilyLoaded { get; set; }
	};
}
