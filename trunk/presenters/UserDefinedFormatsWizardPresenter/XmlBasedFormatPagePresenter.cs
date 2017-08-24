using System.Xml;
using System;

namespace LogJoint.UI.Presenters.FormatsWizard.XmlBasedFormatPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
		readonly IObjectFactory objectsFactory;
		readonly XmlNamespaceManager namespaces;
		ISampleLogAccess sampleLogAccess;
		XmlNode formatRoot;
		XmlNode xmlRoot;
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
			this.namespaces = XmlFormat.UserDefinedFormatFactory.NamespaceManager;
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
				"xml"
			);
			if (testResult == null)
				return;
			testOk = testResult.Value;
			UpdateView();	
		}

		void IViewEvents.OnChangeHeaderReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
				interaction.ShowDialog(xmlRoot, true, sampleLogAccess);
			UpdateView();
		}

		void IViewEvents.OnChangeXsltButtonClicked()
		{
			using (var interation = objectsFactory.CreateXsltEditorDialog())
				interation.ShowDialog(formatRoot, sampleLogAccess);
			UpdateView();
		}

		void IViewEvents.OnConceptsLinkClicked()
		{
			help.ShowHelp("HowXmlParsingWorks.htm");
		}

		void UpdateView()
		{
			InitStatusLabel(ControlId.HeaderReStatusLabel, xmlRoot.SelectSingleNode("head-re[text()!='']", namespaces) != null, CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.XsltStatusLabel, !string.IsNullOrEmpty(xmlRoot.SelectSingleNode("xsl:stylesheet", namespaces)?.InnerXml), CustomFormatPageUtils.GetParameterStatusString);
			InitStatusLabel(ControlId.TestStatusLabel, testOk, CustomFormatPageUtils.GetTestPassedStatusString);
			InitStatusLabel(ControlId.SampleLogStatusLabel, sampleLogAccess.SampleLog != "", CustomFormatPageUtils.GetParameterStatusString);
		}

		void InitStatusLabel(ControlId label, bool statusOk, Func<bool, string> strings)
		{
			view.SetLabelProps(label, strings(statusOk), CustomFormatPageUtils.GetLabelColor(statusOk));
		}
	};
};