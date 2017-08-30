using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using System;

namespace LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
		readonly IObjectFactory objectsFactory;
		XmlNode formatRoot;
		XmlNode reGrammarRoot;
		ISampleLogAccess sampleLogAccess;
		bool testOk;

		public Presenter(
			IView view, 
			IWizardScenarioHost host,
			Help.IPresenter help, 
			ITempFilesManager tempFilesManager,
			IAlertPopup alerts,
			IObjectFactory objectsFactory
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.help = help;
			this.tempFilesManager = tempFilesManager;
			this.alerts = alerts;
			this.objectsFactory = objectsFactory;
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			return true;
		}

		object IWizardPagePresenter.ViewObject => view;

		void IPresenter.SetFormatRoot(XmlElement formatRoot)
		{
			this.formatRoot = formatRoot;
			this.reGrammarRoot = formatRoot.SelectSingleNode("regular-grammar");
			if (this.reGrammarRoot == null)
				this.reGrammarRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("regular-grammar"));
			this.sampleLogAccess = new SampleLogAccess(reGrammarRoot);
			UpdateView();
		}

		void IViewEvents.OnSelectSampleButtonClicked()
		{
			using (var editSampleDialog = objectsFactory.CreateEditSampleDialog())
				editSampleDialog.ShowDialog(sampleLogAccess);
			UpdateView();
		}

		void IViewEvents.OnTestButtonClicked()
		{
			var testResult = CustomFormatPageUtils.TestParsing(
				sampleLogAccess.SampleLog,
				alerts,
				tempFilesManager,
				objectsFactory,
				formatRoot,
				"regular-grammar"
			);
			if (testResult == null)
				return;
			testOk = testResult.Value;
			UpdateView();
		}

		void IViewEvents.OnChangeHeaderReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
				interaction.ShowDialog(reGrammarRoot, true, sampleLogAccess);
			UpdateView();
		}

		void IViewEvents.OnChangeBodyReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
				interaction.ShowDialog(reGrammarRoot, false, sampleLogAccess);
			UpdateView();
		}

		void IViewEvents.OnConceptsLinkClicked()
		{
			help.ShowHelp("HowRegexParsingWorks.htm");
		}

		void IViewEvents.OnChangeFieldsMappingButtonClick()
		{
			List<string> allCaptures = new List<string>();
			allCaptures.AddRange(GetRegExCaptures("head-re"));
			allCaptures.AddRange(GetRegExCaptures("body-re"));
			using (var interaction = objectsFactory.CreateEditFieldsMapping())
			{
				interaction.ShowDialog(reGrammarRoot, allCaptures.ToArray());
				UpdateView();
			}
		}

		void InitStatusLabel(ControlId label, bool statusOk, Func<bool, string> strings)
		{
			view.SetLabelProps(label, strings(statusOk), CustomFormatPageUtils.GetLabelColor(statusOk));
		}

		void UpdateView()
		{
			InitStatusLabel(ControlId.HeaderReStatusLabel, reGrammarRoot.SelectSingleNode("head-re[text()!='']") != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.BodyReStatusLabel, reGrammarRoot.SelectSingleNode("body-re[text()!='']") != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.FieldsMappingLabel, reGrammarRoot.SelectSingleNode("fields-config[field[@name='Time']]") != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.TestStatusLabel, testOk, CustomFormatPageUtils.GetTestPassedStatusString);
			InitStatusLabel(ControlId.SampleLogStatusLabel, sampleLogAccess.SampleLog != "", CustomFormatPageUtils.GetParameterStatusString);
		}

		IEnumerable<string> GetRegExCaptures(string reId)
		{
			bool bodyReMode = reId == "body-re";
			string reText;
			XmlNode reNode = reGrammarRoot.SelectSingleNode(reId);
			if (reNode == null)
			if (bodyReMode)
				reText = "";
			else
				yield break;
			else
				reText = reNode.InnerText;
			if (bodyReMode && string.IsNullOrWhiteSpace(reText))
				reText = RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate;
			Regex re;
			try
			{
				re = new Regex(reText, RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
			}
			catch
			{
				yield break;
			}

			int i = 0;
			foreach (string n in re.GetGroupNames())
				if (i++ > 0)
					yield return n;
		}
	};
};