using System.IO;

namespace LogJoint.UI.Presenters.FormatsWizard.FormatDeleteConfirmPage
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

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            return true;
        }

        object IWizardPagePresenter.ViewObject => view;

        void IPresenter.UpdateView(IUserDefinedFactory factory)
        {
            view.Update(
                messageLabelText: string.Format("You are about to delete '{0}' format definition. Press Finish to delete, Cancel to cancel the operation.",
                    LogProviderFactoryRegistry.ToString(factory)),
                descriptionTextBoxValue: factory.FormatDescription,
                fileNameTextBoxValue: factory.Location,
                dateTextBoxValue: File.Exists(factory.Location) ? File.GetLastWriteTime(factory.Location).ToString() : ""
            );
        }
    };
};