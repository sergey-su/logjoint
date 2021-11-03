using LogJoint.UI.Presenters;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint.Wasm.UI
{
    public class MainFormViewProxy : IView
    {
        void IView.BeginSplittingSearchResults()
        {
        }

        void IView.BeginSplittingTabsPanel()
        {
        }

        IInputFocusState IView.CaptureInputFocusState() => null;

        void IView.Close()
        {
        }

        void IView.EnableFormControls(bool enable)
        {
        }

        void IView.EnableOwnedForms(bool enable)
        {
        }

        void IView.ExecuteThreadPropertiesDialog(IThread thread, IPresentersFacade navHandler, IColorTheme theme)
        {
        }

        void IView.ForceClose()
        {
        }

        void IView.SetAnalyzingIndicationVisibility(bool value)
        {
        }

        void IView.SetCaption(string value)
        {
        }

        void IView.SetIssueReportingMenuAvailablity(bool value)
        {
        }

        void IView.SetShareButtonState(bool visible, bool enabled, bool progress)
        {
        }

        void IView.SetTaskbarState(TaskbarState state)
        {
        }

        void IView.SetViewModel(IViewModel value)
        {
        }

        void IView.ShowOptionsMenu()
        {
        }

        void IView.UpdateTaskbarProgress(int progressPercentage)
        {
        }
    }
}
