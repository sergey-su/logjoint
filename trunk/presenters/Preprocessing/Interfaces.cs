using System;
using System.Collections.Generic;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Presenters.PreprocessingUserInteractions
{
    public interface IPresenter
    {
    };

    public interface IView
    {
        void SetViewModel(IViewModel viewModel);
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        DialogViewData DialogData { get; }
        void OnCheck(IDialogItem item, bool value);
        void OnSelect(IDialogItem item);
        void OnCloseDialog(bool accept);
        void OnCheckAll(bool value);
    };

    public class DialogViewData
    {
        public string Title { get; internal set; }
        public IReadOnlyList<IDialogItem> Items { get; internal set; }
    };

    public interface IDialogItem : IListItem
    {
        string Title { get; }
        bool IsChecked { get; }
    };
}
