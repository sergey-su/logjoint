using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
    [Flags]
    public enum AlertFlags
    {
        None = 0,
        Ok = 1,
        Yes = 2,
        No = 4,
        Cancel = 8,
        YesNoCancel = Yes | No | Cancel,
        ButtonsMask = 0xff,

        WarningIcon = 512,
        QuestionIcon = 1024,
        IconsMask = 0xff00
    };

    public interface IAlertPopup
    {
        AlertFlags ShowPopup(string caption, string text, AlertFlags flags);
        Task<AlertFlags> ShowPopupAsync(string caption, string text, AlertFlags flags);
    }

    namespace AlertPopup
    {
        public interface IViewModel
        {
            IChangeNotification ChangeNotification { get; }
            IViewState ViewState { get; } // if null, the popup is not visible
            void OnButtonClicked(AlertFlags button);
            void OnClickOutside();
        };

        public interface IViewState
        {
            string Caption { get; }
            string Text { get; }
            AlertFlags Buttons { get; }
            AlertFlags Icon { get; }
        };
    }
}

