using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard.ImportNLogPage
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

		string IPresenter.Pattern => view.PatternTextBoxValue;

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
			XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
			nsMgr.AddNamespace("nlog", "http://www.nlog-project.org/schemas/NLog.xsd");
			foreach (XmlElement e in doc.SelectNodes("//nlog:target[@layout]", nsMgr))
			{
				AvailablePattern p = new AvailablePattern();
				string value = e.GetAttribute("layout");
				if (string.IsNullOrWhiteSpace(value))
					continue;
				p.Value = value;
				p.AppenderName = e.GetAttribute("name");
				if (string.IsNullOrWhiteSpace(p.AppenderName))
					p.AppenderName = "unnamed";
				avaPatterns.Add(p);
			}
			if (avaPatterns.Count == 0)
			{
				alerts.ShowPopup("Error", "No layout patterns found in the config", AlertFlags.Ok | AlertFlags.WarningIcon);
				return;
			}

			view.SetAvailablePatternsListBoxItems(avaPatterns.Select(i => i.ToString()).ToArray());
			view.ConfigFileTextBoxValue = fileName;
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

		bool IPresenter.ValidateInput()
		{
			if (view.PatternTextBoxValue.Length == 0)
			{
				alerts.ShowPopup("Validation", "Provide a layout string, please", AlertFlags.Ok | AlertFlags.WarningIcon);
				return false;
			}
			return true;
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