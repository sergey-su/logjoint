using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer
{
    public interface IPresenterInternal : IPresenter
    {
        bool IsObjectEventPresented(ILogSource source, TextLogEventTrigger eventTrigger);
        bool TrySelectObject(ILogSource source, TextLogEventTrigger objectEvent, Func<IVisualizerNode, int> disambiguationFunction);
    };

    public interface IView
    {
        void SetViewModel(IViewModel value);
        void Show();

        void ScrollStateHistoryItemIntoView(int itemIndex);
    };

    public interface IObjectsTreeNode : ITreeNode
    {
    };

    public enum NodeColoring
    {
        Alive = 1,
        Deleted = 2,
        NotCreatedYet = 3,
        LogSource = 4, // color of the first involved log source
    };

    public enum Key
    {
        None,
        Enter,
        BookmarkShortcut,
        CopyShortcut,
    };

    public interface IViewModel
    {
        ColorThemeMode ColorTheme { get; }

        IChangeNotification ChangeNotification { get; }

        void OnVisibleChanged(bool value);

        IObjectsTreeNode ObjectsTreeRoot { get; }
        PaintNodeDelegate PaintNode { get; }
        void OnExpandNode(IObjectsTreeNode node);
        void OnCollapseNode(IObjectsTreeNode node);
        void OnSelect(IReadOnlyCollection<IObjectsTreeNode> value);
        MenuData OnNodeMenuOpening();
        void OnNodeDeleteKeyPressed();
        double? HistorySize { get; }
        double? ObjectsTreeSize { get; }


        string CurrentTimeLabelText { get; }

        IReadOnlyList<KeyValuePair<string, object>> ObjectsProperties { get; }
        void OnPropertiesRowDoubleClicked(int rowIndex);
        PropertyCellPaintInfo OnPropertyCellPaint(int rowIndex);
        void OnPropertyCellClicked(int rowIndex);
        void OnPropertyCellCopyShortcutPressed();
        // Below are reactive-friendly version of properties API above
        IReadOnlyList<IPropertyListItem> PropertyItems { get; }
        void OnSelectProperty(IPropertyListItem property);
        void OnPropertyDoubleClicked(IPropertyListItem property);
        void OnPropertyCellClicked(IPropertyListItem property);


        IReadOnlyList<IStateHistoryItem> ChangeHistoryItems { get; }
        Predicate<IStateHistoryItem> IsChangeHistoryItemBookmarked { get; }
        FocusedMessageInfo FocusedMessagePositionInChangeHistory { get; }
        void OnChangeHistoryItemDoubleClicked(IStateHistoryItem item);
        void OnChangeHistoryItemKeyEvent(IStateHistoryItem item, Key key);
        void OnChangeHistoryChangeSelection(IEnumerable<IStateHistoryItem> items);
        void OnFindCurrentPositionInChangeHistory();
        void OnResizeHistory(double value);
        void OnResizeObjectsTree(double value);

        InlineSearch.IViewModel InlineSearch { get; }
        ToastNotificationPresenter.IViewModel ToastNotification { get; }
        bool IsNotificationsIconVisibile { get; }
        void OnActiveNotificationButtonClicked();

        void OnSearchShortcutPressed();

        string ObjectDescription { get; }

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
        public Drawing.Color? LogSourceColor;
        public string Annotation;
    };

    public delegate NodePaintInfo PaintNodeDelegate(IObjectsTreeNode node, bool getPrimaryPropValue);

    public interface IStateHistoryItem : IListItem
    {
        string Time { get; }
        string Message { get; }
        int Index { get; }
    };

    public enum PropertyLinkType
    {
        None,
        Internal,
        External,
    };

    public interface IPropertyListItem : IListItem
    {
        string Name { get; }
        string Value { get; }
        PropertyLinkType LinkType { get; }
        bool IsLeftPadded { get; }
    };
}
