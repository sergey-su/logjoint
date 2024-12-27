using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.PromptDialog
{
    public class Presenter: IPromptDialog, IViewModel
    {
        readonly IChangeNotification changeNotification;
        ViewState viewState;

        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        string IPromptDialog.ExecuteDialog(string caption, string prompt, string defaultValue)
        {
            throw new NotImplementedException("Sync prompt is not supported");
        }

        Task<string> IPromptDialog.ExecuteDialogAsync(string caption, string prompt, string defaultValue)
        {
            EnsureHidden(null);
            viewState = new ViewState()
            {
                caption = caption,
                prompt = prompt,
                value = defaultValue,
                taskSource = new TaskCompletionSource<string>(),
            };
            changeNotification.Post();
            return viewState.taskSource.Task;
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        IViewState IViewModel.ViewState => viewState;

        void IViewModel.OnConfirm() => EnsureHidden(viewState.value);

        void IViewModel.OnCancel() => EnsureHidden(null);

        void IViewModel.OnInput(string value)
        {
            viewState.value = value;
            changeNotification.Post();
        }

        void EnsureHidden(string value)
        {
            ViewState tmp = viewState;
            if (tmp == null)
                return;
            viewState = null;
            tmp.taskSource.SetResult(value);
            changeNotification.Post();
        }

        class ViewState : IViewState
        {
            public string caption, prompt;
            public string value;
            public TaskCompletionSource<string> taskSource;

            string IViewState.Caption => caption;
            string IViewState.Prompt => prompt;
            string IViewState.Value => value;
        };
    }
}
