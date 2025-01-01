namespace LogJoint.UI.Presenters.FormatsWizard.ChooseOperationPage
{
    internal class Presenter : IPresenter, IViewEvents
    {
        readonly IView view;
        readonly IWizardScenarioHost host;

        public Presenter(IView view, IWizardScenarioHost host)
        {
            this.view = view;
            this.view.SetEventsHandler(this);
            this.host = host;
        }

        void IViewEvents.OnOptionDblClicked()
        {
            host.Next();
        }

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            return true;
        }

        object IWizardPagePresenter.ViewObject => view;

        ControlId IPresenter.SelectedControl => view.SelectedControl;
    };
};