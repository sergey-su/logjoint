using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.UI.Presenters.Postprocessing.SummaryDialog
{
    public struct ViewItem
    {
        public enum ItemKind
        {
            LogSource,
            Issue
        };
        public ItemKind Kind;
        public string Text;
        public object LinkData;
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        bool IsVisible { get; }
        IReadOnlyList<ViewItem> Items { get; }
        void OnCancel();
        void OnLinkClicked(object linkData);
    };

    public interface IPresenter
    {
        void Enable();
        bool IsEnabled { get; }
        void ShowDialog(IEnumerable<(ILogSource logSource, IStructuredPostprocessorRunSummary summary)> summaries);
    };
}
