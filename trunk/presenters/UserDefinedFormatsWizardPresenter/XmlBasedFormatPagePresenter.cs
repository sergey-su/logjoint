using System.Xml;
using System;

using IGenericView = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.IView;
using IGenericViewEvents = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.IViewEvents;
using ControlId = LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage.ControlId;

namespace LogJoint.UI.Presenters.FormatsWizard.XmlBasedFormatPage
{
	internal class Presenter : IPresenter, IGenericViewEvents
	{
		readonly IGenericView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITestParsing testParsing;
		readonly IFactory objectsFactory;
		readonly XmlNamespaceManager namespaces;
		ISampleLogAccess sampleLogAccess;
		XmlNode formatRoot;
		XmlNode xmlRoot;
		bool testOk;

		public Presenter(
			IGenericView view, 
			IWizardScenarioHost host,
			Help.IPresenter help, 
			ITestParsing testParsing,
			IFactory objectsFactory
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.help = help;
			this.testParsing = testParsing;
			this.objectsFactory = objectsFactory;
			this.namespaces = XmlFormat.UserDefinedFormatFactory.NamespaceManager;
			InitLabel(ControlId.PageTitleLabel, "Provide the data needed to parse your XML logs");
			InitLabel(ControlId.ConceptsLabel, "Learn how LogJoint uses regular expressions and XSLT to parse XML logs");
			InitLabel(ControlId.SampleLogLabel, "Select sample log file that can help you test your XML format configuration");
			InitLabel(ControlId.HeaderReLabel, "Construct header regular expression");
			InitLabel(ControlId.TransformLabel, "Compose XSL transformation");
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
			this.xmlRoot = formatRoot.SelectSingleNode("xml");
			if (this.xmlRoot == null)
				this.xmlRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("xml"));
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
			var testResult = testParsing.Test(
				sampleLogAccess.SampleLog,
				formatRoot,
				"xml"
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
			using (var interation = objectsFactory.CreateXsltEditorDialog())
				interation.ShowDialog(formatRoot, sampleLogAccess);
			UpdateView();
		}

		void IGenericViewEvents.OnConceptsLinkClicked()
		{
			help.ShowHelp("HowXmlParsingWorks.htm");
		}

		void UpdateView()
		{
			InitStatusLabel(ControlId.HeaderReStatusLabel, xmlRoot.SelectSingleNode("head-re[text()!='']", namespaces) != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.TransformStatusLabel, !string.IsNullOrEmpty(xmlRoot.SelectSingleNode("xsl:stylesheet", namespaces)?.InnerXml), CustomFormatPageUtils.GetParameterStatusString);
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