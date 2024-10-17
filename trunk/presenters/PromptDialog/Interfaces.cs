using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
    namespace PromptDialog
    {
        public interface IViewModel
        {
            IChangeNotification ChangeNotification { get; }
            IViewState ViewState { get; } // if null, the popup is not visible
            void OnConfirm();
            void OnCancel();
            void OnInput(string value);
        };

        public interface IViewState
        {
            string Caption { get; }
            string Prompt { get; }
            string Value { get; }
        };
    }
}
