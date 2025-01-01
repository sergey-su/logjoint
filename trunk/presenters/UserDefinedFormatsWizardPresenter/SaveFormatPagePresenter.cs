using System;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard.SaveFormatPage
{
    internal class Presenter : IPresenter, IViewEvents
    {
        readonly IView view;
        readonly IWizardScenarioHost host;
        readonly IFormatDefinitionsRepository repo;
        readonly IAlertPopup alerts;
        readonly bool newFormatMode;

        XmlDocument doc;

        public Presenter(
            IView view,
            IWizardScenarioHost host,
            IAlertPopup alerts,
            IFormatDefinitionsRepository repo,
            bool newFormatMode
        )
        {
            this.view = view;
            this.view.SetEventsHandler(this);
            this.host = host;
            this.alerts = alerts;
            this.newFormatMode = newFormatMode;
            this.repo = repo;
            UpdateView();
        }

        public void SetDocument(XmlDocument doc)
        {
            this.doc = doc;
        }

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            if (!movingForward)
                return true;

            if (!ValidateInput())
                return false;

            try
            {
                doc.Save(view.FileNameTextBoxValue);
            }
            catch (Log4NetImportException e)
            {
                alerts.ShowPopup("Error", "Failed to save the format:\n" + e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
                return false;
            }
            return true;
        }

        object IWizardPagePresenter.ViewObject => view;

        bool ValidateInput()
        {
            string basis = GetValidBasis();
            if (basis == null)
            {
                alerts.ShowPopup("Validation", "File name basis is invalid.", AlertFlags.Ok | AlertFlags.WarningIcon);
                return false;
            }
            string fname = GetFullFormatFileName(basis);
            if (newFormatMode && System.IO.File.Exists(fname))
            {
                if (alerts.ShowPopup("Validation", "File already exists. Overwrite?", AlertFlags.YesNoCancel | AlertFlags.WarningIcon) != AlertFlags.Yes)
                {
                    return false;
                }
            }
            return true;
        }

        string GetFullFormatFileName(string basis)
        {
            DirectoryFormatsRepository directoryRepo = repo as DirectoryFormatsRepository;
            if (directoryRepo != null)
                return directoryRepo.GetFullFormatFileName(basis);
            return basis;
        }

        string IPresenter.FileNameBasis
        {
            get { return view.FileNameBasisTextBoxValue; }
            set { view.FileNameBasisTextBoxValue = value; UpdateView(); }
        }

        string GetValidBasis()
        {
            string ret = view.FileNameBasisTextBoxValue;
            ret = ret.Trim();
            if (ret == "")
                return null;
            if (ret.IndexOfAny(new char[] { '\\', '/', ':', '"', '<', '>', '|', '?', '*' }) >= 0)
                return null;
            return ret;
        }

        void UpdateView()
        {
            string basis = GetValidBasis();
            if (basis == null)
                view.FileNameTextBoxValue = "";
            else
                view.FileNameTextBoxValue = GetFullFormatFileName(basis);
        }

        void IViewEvents.OnFileNameBasisTextBoxChanged()
        {
            UpdateView();
        }
    };
};