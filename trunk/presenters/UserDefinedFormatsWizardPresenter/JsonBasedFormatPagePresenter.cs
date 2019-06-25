using System.Xml;
using System;

using IGenericView = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.IView;
using IGenericViewEvents = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.IViewEvents;
using ControlId = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.ControlId;

namespace LogJoint.UI.Presenters.FormatsWizard.JsonBasedFormatPage
{
	internal class Presenter : IPresenter, IGenericViewEvents
	{
		readonly IGenericView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
		readonly IFactory objectsFactory;
		ISampleLogAccess sampleLogAccess;
		XmlNode formatRoot;
		XmlNode xmlRoot;
		bool testOk;

		public Presenter(
			IGenericView view, 
			IWizardScenarioHost host,
			Help.IPresenter help, 
			ITempFilesManager tempFilesManager,
			IAlertPopup alerts,
			IFactory objectsFactory
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.help = help;
			this.tempFilesManager = tempFilesManager;
			this.alerts = alerts;
			this.objectsFactory = objectsFactory;
			InitLabel(ControlId.PageTitleLabel, "Provide the data needed to parse your JSON logs");
			InitLabel(ControlId.ConceptsLabel, "Learn how LogJoint uses regular expressions and JUST transformation to parse JSON logs");
			InitLabel(ControlId.SampleLogLabel, "Select sample log file that can help you test your JSON format configuration");
			InitLabel(ControlId.HeaderReLabel, "Construct header regular expression");
			InitLabel(ControlId.TransformLabel, "Compose JUST tranformation");
			InitLabel(ControlId.TestLabel, "Test the data you provided. Click \"Test\" to extract the messages from sample file.");
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			return true;
		}

		object IWizardPagePresenter.ViewObject => view;

		void IPresenter.SetFormatRoot(XmlElement formatRoot)
		{
			this.formatRoot = formatRoot;
			this.xmlRoot = formatRoot.SelectSingleNode("json");
			if (this.xmlRoot == null)
				this.xmlRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("json"));
			this.sampleLogAccess = new SampleLogAccess(xmlRoot);
			UpdateView();
		}

		void IGenericViewEvents.OnSelectSampleButtonClicked()
		{
			using (var editSampleDialog = objectsFactory.CreateEditSampleDialog())
				editSampleDialog.ShowDialog(sampleLogAccess);
			UpdateView();
		}

		void IGenericViewEvents.OnTestButtonClicked()
		{
			var testResult = CustomFormatPageUtils.TestParsing(
				sampleLogAccess.SampleLog,
				alerts,
				tempFilesManager,
				objectsFactory,
				formatRoot,
				"json"
			);
			if (testResult == null)
				return;
			testOk = testResult.Value;
			UpdateView();	
		}

		void IGenericViewEvents.OnChangeHeaderReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
				interaction.ShowDialog(xmlRoot, true, sampleLogAccess);
			UpdateView();
		}

		void IGenericViewEvents.OnChangeTransformButtonClicked()
		{
			using (var interation = objectsFactory.CreateJUSTEditorDialog())
				interation.ShowDialog(formatRoot, sampleLogAccess);
			UpdateView();
		}

		void IGenericViewEvents.OnConceptsLinkClicked()
		{
			help.ShowHelp("HowJsonParsingWorks.htm");
		}

		void UpdateView()
		{
			InitStatusLabel(ControlId.HeaderReStatusLabel, xmlRoot.SelectSingleNode("head-re[text()!='']") != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.TransformStatusLabel, !string.IsNullOrEmpty(xmlRoot.SelectSingleNode("transform")?.InnerXml), CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.TestStatusLabel, testOk, CustomFormatPageUtils.GetTestPassedStatusString);
			InitStatusLabel(ControlId.SampleLogStatusLabel, sampleLogAccess.SampleLog != "", CustomFormatPageUtils.GetParameterStatusString);
		}

		void InitStatusLabel(ControlId label, bool statusOk, Func<bool, string> strings)
		{
			view.SetLabelProps(label, strings(statusOk), CustomFormatPageUtils.GetLabelColor(statusOk));
		}

		void InitLabel(ControlId label, string value)
		{
			view.SetLabelProps(label, value, null);
		}
	};
};