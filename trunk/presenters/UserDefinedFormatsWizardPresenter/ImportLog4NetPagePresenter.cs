using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard.ImportLog4NetPage
{
    internal class Presenter : IPresenter, IViewEvents
    {
        readonly IView view;
        readonly IWizardScenarioHost host;
        readonly IAlertPopup alerts;
        readonly IFileDialogs fileDialogs;
        List<AvailablePattern> avaPatterns;

        public Presenter(
            IView view,
            IWizardScenarioHost host,
            IAlertPopup alerts,
            IFileDialogs fileDialogs
        )
        {
            this.view = view;
            this.view.SetEventsHandler(this);
            this.host = host;
            this.alerts = alerts;
            this.fileDialogs = fileDialogs;
        }

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            return true;
        }

        object IWizardPagePresenter.ViewObject => view;

        bool IPresenter.GenerateGrammar(XmlElement root)
        {
            try
            {
                Log4NetPatternImporter.GenerateRegularGrammarElement(root, view.PatternTextBoxValue);
            }
            catch (Log4NetImportException e)
            {
                alerts.ShowPopup("Error", "Failed to import the pattern:\n" + e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
                return false;
            }
            return true;
        }

        bool IPresenter.ValidateInput()
        {
            if (view.PatternTextBoxValue.Length == 0)
            {
                alerts.ShowPopup("Validation", "Provide a pattern string, please", AlertFlags.Ok | AlertFlags.WarningIcon);
                return false;
            }
            return true;
        }

        void IViewEvents.OnOpenConfigButtonClicked()
        {
            string fileName = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
            {
                Filter = "Config files (*.config)|*.config",
                CanChooseFiles = true,
                AllowsMultipleSelection = false
            })?.FirstOrDefault();
            if (fileName == null)
                return;

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(fileName);
            }
            catch (XmlException e)
            {
                alerts.ShowPopup("Error", "Failed to load the config: " + e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
                return;
            }
            avaPatterns = new List<AvailablePattern>();
            foreach (XmlElement e in doc.SelectNodes("configuration/log4net/appender[@name]/layout[@type='log4net.Layout.PatternLayout'][@value | conversionPattern[@value]]"))
            {
                AvailablePattern p = new AvailablePattern();
                XmlNode valueNode = e.SelectSingleNode("@value | conversionPattern/@value");
                if (valueNode == null)
                    continue;
                p.Value = valueNode.Value;
                if (string.IsNullOrEmpty(p.Value))
                    continue;
                p.AppenderName = ((XmlElement)e.ParentNode).GetAttribute("name");
                avaPatterns.Add(p);
            }
            if (avaPatterns.Count == 0)
            {
                alerts.ShowPopup("Error", "No layout patterns found in the config", AlertFlags.Ok | AlertFlags.WarningIcon);
                return;
            }

            view.SetAvailablePatternsListItems(avaPatterns.Select(p => p.ToString()).ToArray());
            view.SetConfigFileTextBoxValue(fileName);
        }

        void IViewEvents.OnSelectedAvailablePatternChanged(int idx)
        {
            var pat = avaPatterns?.ElementAtOrDefault(idx);
            if (pat != null)
                view.PatternTextBoxValue = pat.Value;
        }

        void IViewEvents.OnSelectedAvailablePatternDoubleClicked()
        {
            if (view.PatternTextBoxValue.Length > 0)
                host.Next();
        }

        class AvailablePattern
        {
            public string AppenderName;
            public string Value;
            public override string ToString()
            {
                return string.Format("{0}: {1}", AppenderName, Value);
            }
        };
    };
};