using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using LogJoint.NLog;

namespace LogJoint.UI.Presenters.FormatsWizard.ImportNLogPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		List<AvailableLayout> avaPatterns;
		int selectedAvailablePattern = -1;

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

		ISelectedLayout IPresenter.GetSelectedLayout()
		{
			var pat = avaPatterns?.ElementAtOrDefault(selectedAvailablePattern);
			var editorValue = view.PatternTextBoxValue;
			if (pat != null && pat.GetLayoutEditorValue() == editorValue)
				return pat;
			return new SimpleLayout() { Value = editorValue, TargetName = "" };
		}

		void IViewEvents.OnOpenConfigButtonClicked()
		{
			string fileName = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
			{
				Filter = "Config files (*.config)|*.config",
				CanChooseFiles = true,
				CanChooseDirectories = false,
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
			avaPatterns = new List<AvailableLayout>();

			Func<XmlElement, string> getTargetName = e =>
			{
				var name = e.GetAttribute("name");
				if (string.IsNullOrWhiteSpace(name))
					name = "unnamed <target>";
				return name;
			};

			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target'][@layout]"))
			{
				avaPatterns.Add(new SimpleLayout()
				{
					Value = e.GetAttribute("layout"),
					TargetName = getTargetName(e)
				});
			}

			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target']/*[local-name()='layout' and @*[local-name()='type']='CsvLayout']"))
			{
				avaPatterns.Add(new CsvLayout()
				{
					Params = new NLog.CsvParams(e),
					TargetName = getTargetName((XmlElement)e.ParentNode)
				});
			}

			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target']/*[local-name()='layout' and @*[local-name()='type']='JsonLayout']"))
			{
				avaPatterns.Add(new JsonLayout()
				{
					Params = new NLog.JsonParams(e),
					TargetName = getTargetName((XmlElement)e.ParentNode)
				});
			}

			if (avaPatterns.Count == 0)
			{
				alerts.ShowPopup("Error", "No layout patterns found in the config", AlertFlags.Ok | AlertFlags.WarningIcon);
				return;
			}

			view.SetAvailablePatternsListBoxItems(avaPatterns.Select(i => i.ToString()).ToArray());
			selectedAvailablePattern = -1;
			view.ConfigFileTextBoxValue = fileName;
		}

		void IViewEvents.OnSelectedAvailablePatternChanged(int idx)
		{
			selectedAvailablePattern = idx;
			var pat = avaPatterns?.ElementAtOrDefault(idx);
			if (pat != null)
				view.PatternTextBoxValue = pat.GetLayoutEditorValue();
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

		abstract class AvailableLayout: ISelectedLayout
		{
			public string TargetName;
			public abstract string GetLayoutEditorValue();
		};

		class SimpleLayout: AvailableLayout, ISimpleLayout
		{
			public string Value { get; set; }

			public override string ToString()
			{
				return string.Format("{0}: {1}", TargetName, Value);
			}

			public override string GetLayoutEditorValue()
			{
				return Value;
			}
		};

		class CsvLayout: AvailableLayout, ICSVLayout
		{
			public NLog.CsvParams Params { get; set; }

			public override string ToString()
			{
				return string.Format("{0}: CSV layout, {1} columns", TargetName, Params.ColumnLayouts.Count);
			}

			public override string GetLayoutEditorValue()
			{
				return string.Format("(CSV layout with {0} columns)", Params.ColumnLayouts.Count);
			}
		};

		class JsonLayout: AvailableLayout, IJsonLayout
		{
			public NLog.JsonParams Params { get; set; }

			public override string ToString()
			{
				return string.Format("{0}: Json layout, {1} attributes", TargetName, Params.Root.Attrs.Count);
			}

			public override string GetLayoutEditorValue()
			{
				return string.Format("(Json layout with {0} attributes)", Params.Root.Attrs.Count);
			}
		};
	};
};