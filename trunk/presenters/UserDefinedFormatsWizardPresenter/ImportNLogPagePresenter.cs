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
			var editorValue = pat.GetLayoutEditorValue();
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
			XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
			nsMgr.AddNamespace("nlog", "http://www.nlog-project.org/schemas/NLog.xsd");
			nsMgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

			Func<XmlElement, string> getTargetName = e =>
			{
				var name = e.GetAttribute("name");
				if (string.IsNullOrWhiteSpace(name))
					name = "unnamed <target>";
				return name;
			};

			// simple layouts
			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target'][@layout]"))
			{
				var p = new SimpleLayout();
				string value = e.GetAttribute("layout");
				if (string.IsNullOrWhiteSpace(value))
					continue;
				p.Value = value;
				p.TargetName = getTargetName(e);
				avaPatterns.Add(p);
			}

			// csv layouts
			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target']/*[local-name()='layout' and @*[local-name()='type']='CsvLayout']"))
			{
				var p = new CsvLayout();
				p.Params = new NLog.LayoutImporter.CsvParams();
				p.Params.Load(e);
				if (p.Params.ColumnLayouts.Count == 0)
					continue;
				p.TargetName = getTargetName((XmlElement)e.ParentNode);
				avaPatterns.Add(p);
			}

			// json layouts
			foreach (XmlElement e in doc.SelectNodes("//*[local-name()='target']/*[local-name()='layout' and @*[local-name()='type']='JsonLayout']"))
			{
				var p = new JsonLayout();
				p.Params = new NLog.LayoutImporter.JsonParams();
				p.Params.Load(e);
				if ((p.Params.Root?.Attrs?.Count).GetValueOrDefault() == 0)
					continue;
				p.TargetName = getTargetName((XmlElement)e.ParentNode);
				avaPatterns.Add(p);
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
			public NLog.LayoutImporter.CsvParams Params { get; set; }

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
			public NLog.LayoutImporter.JsonParams Params { get; set; }

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