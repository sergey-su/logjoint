using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.FilterDialog
{
    public class NameEditBoxProperties
    {
        public string Value;
        public bool Enabled;
        public string LinkText;
    };

    public class DialogConfig
    {
        public string Title;
        public KeyValuePair<string, Color?>[] ActionComboBoxOptions;
    };

    public interface IPresenter
    {
        Task<bool> ShowTheDialog(IFilter forFilter, FiltersListPurpose filtersListPurpose);
    };

    public interface IScopeItem : IListItem
    {
        int Indent { get; }
        bool IsChecked { get; }
    };

    public interface IMessageTypeItem : IListItem
    {
        bool IsChecked { get; }
    };

    [Flags]
    public enum CheckBoxId
    {
        None = 0,
        FilterEnabled = 1,
        MatchCase = 2,
        RegExp = 4,
        WholeWord = 8,
    };

    public class TimeRangeBoundProperties(bool enabled, DateTime value)
    {
        public bool Enabled { get; private set; } = enabled;
        public DateTime Value { get; private set; } = value;
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        bool IsVisible { get; }
        DialogConfig Config { get; }
        IReadOnlyList<IScopeItem> ScopeItems { get; }
        IReadOnlyList<IMessageTypeItem> MessageTypeItems { get; }
        CheckBoxId CheckedBoxes { get; }
        NameEditBoxProperties NameEdit { get; }
        TimeRangeBoundProperties BeginTimeBound { get; }
        TimeRangeBoundProperties EndTimeBound { get; }
        string Template { get; }
        int ActionComboBoxValue { get; }
        void SetView(IView view);
        void OnScopeItemCheck(IScopeItem item, bool checkedValue);
        void OnScopeItemSelect(IScopeItem item);
        void OnMessageTypeItemCheck(IMessageTypeItem item, bool checkedValue);
        void OnMessageTypeItemSelect(IMessageTypeItem item);
        void OnNameEditLinkClicked();
        void OnConfirmed();
        void OnCancelled();
        void OnCheckBoxCheck(CheckBoxId cb, bool checkedValue);
        void OnNameChange(string value);
        void OnTemplateChange(string value);
        void OnActionComboBoxValueChange(int value);
    };

    public interface IView
    {
        void PutFocusOnNameEdit();
    };
};