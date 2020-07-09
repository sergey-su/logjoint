using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class Factory : IFactory
	{
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly ILogProviderFactoryRegistry registry;
		readonly IFormatDefinitionsRepository repo;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly RegularExpressions.IRegexFactory regexFactory;
		readonly LogViewer.IPresenterFactory logViewerPresenterFactory;
		readonly IViewsFactory viewFactories;
		readonly ISynchronizationContext synchronizationContext;
		readonly FieldsProcessor.IFactory fieldsProcessorFactory;
		readonly LogMedia.IFileSystem fileSystem;

		public interface IViewsFactory
		{
			IView CreateFormatsWizardView();
			ChooseOperationPage.IView CreateChooseOperationPageView();
			ImportLog4NetPage.IView CreateImportLog4NetPagePageView();
			FormatIdentityPage.IView CreateFormatIdentityPageView();
			FormatAdditionalOptionsPage.IView CreateFormatAdditionalOptionsPage();
			SaveFormatPage.IView CreateSaveFormatPageView();
			NLogGenerationLogPage.IView CreateNLogGenerationLogPageView();
			ImportNLogPage.IView CreateImportNLogPage();
			ChooseExistingFormatPage.IView CreateChooseExistingFormatPageView();
			FormatDeleteConfirmPage.IView CreateFormatDeleteConfirmPageView();
			RegexBasedFormatPage.IView CreateRegexBasedFormatPageView();
			EditSampleDialog.IView CreateEditSampleDialogView();
			TestDialog.IView CreateTestDialogView();
			EditRegexDialog.IView CreateEditRegexDialog();
			EditFieldsMapping.IView CreateEditFieldsMappingDialog();
			CustomTransformBasedFormatPage.IView CreateXmlBasedFormatPageView();
			CustomTransformBasedFormatPage.IView CreateJsonBasedFormatPageView();
			CustomCodeEditorDialog.IView CreateXsltEditorDialog();
			CustomCodeEditorDialog.IView CreateJUSTEditorDialog();
		};

		public Factory(
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			Help.IPresenter help,
			ILogProviderFactoryRegistry registry,
			IFormatDefinitionsRepository repo,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ITempFilesManager tempFilesManager,
			ITraceSourceFactory traceSourceFactory,
			RegularExpressions.IRegexFactory regexFactory,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			IViewsFactory viewFactories,
			ISynchronizationContext synchronizationContext,
			FieldsProcessor.IFactory fieldsProcessorFactory,
			LogMedia.IFileSystem fileSystem
		)
		{
			this.viewFactories = viewFactories;
			this.alerts = alerts;
			this.registry = registry;
			this.fileDialogs = fileDialogs;
			this.userDefinedFormatsManager = userDefinedFormatsManager;
			this.help = help;
			this.repo = repo;
			this.tempFilesManager = tempFilesManager;
			this.logViewerPresenterFactory = logViewerPresenterFactory;
			this.traceSourceFactory = traceSourceFactory;
			this.regexFactory = regexFactory;
			this.synchronizationContext = synchronizationContext;
			this.fieldsProcessorFactory = fieldsProcessorFactory;
			this.fileSystem = fileSystem;
		}

		IView IFactory.CreateWizardView()
		{
			return viewFactories.CreateFormatsWizardView();
		}

		ChooseOperationPage.IPresenter IFactory.CreateChooseOperationPage(IWizardScenarioHost host)
		{
			return new ChooseOperationPage.Presenter(viewFactories.CreateChooseOperationPageView(), host);
		}

		IFormatsWizardScenario IFactory.CreateRootScenario(IWizardScenarioHost host)
		{
			return new RootScenario(host, this);
		}

		IFormatsWizardScenario IFactory.CreateImportLog4NetScenario(IWizardScenarioHost host)
		{
			return new ImportLog4NetScenario(host, this);
		}

		ImportLog4NetPage.IPresenter IFactory.CreateImportLog4NetPage(IWizardScenarioHost host)
		{
			return new ImportLog4NetPage.Presenter(viewFactories.CreateImportLog4NetPagePageView(), host, alerts, fileDialogs);
		}

		FormatIdentityPage.IPresenter IFactory.CreateFormatIdentityPage(IWizardScenarioHost host, bool newFormatMode)
		{
			return new FormatIdentityPage.Presenter(viewFactories.CreateFormatIdentityPageView(), host, alerts, registry, newFormatMode);
		}

		FormatAdditionalOptionsPage.IPresenter IFactory.CreateFormatAdditionalOptionsPage(IWizardScenarioHost host)
		{
			return new FormatAdditionalOptionsPage.Presenter(viewFactories.CreateFormatAdditionalOptionsPage(), host, help);
		}

		SaveFormatPage.IPresenter IFactory.CreateSaveFormatPage(IWizardScenarioHost host, bool newFormatMode)
		{
			return new SaveFormatPage.Presenter(viewFactories.CreateSaveFormatPageView(), host, alerts, repo, newFormatMode);
		}

		NLogGenerationLogPage.IPresenter IFactory.CreateNLogGenerationLogPage(IWizardScenarioHost host)
		{
			return new NLogGenerationLogPage.Presenter(viewFactories.CreateNLogGenerationLogPageView(), host);
		}

		ImportNLogPage.IPresenter IFactory.CreateImportNLogPage(IWizardScenarioHost host)
		{
			return new ImportNLogPage.Presenter(viewFactories.CreateImportNLogPage(), host, alerts, fileDialogs);
		}

		IFormatsWizardScenario IFactory.CreateImportNLogScenario(IWizardScenarioHost host)
		{
			return new ImportNLogScenario(host, this, alerts);
		}

		ChooseExistingFormatPage.IPresenter IFactory.CreateChooseExistingFormatPage(IWizardScenarioHost host)
		{
			return new ChooseExistingFormatPage.Presenter(viewFactories.CreateChooseExistingFormatPageView(), host, userDefinedFormatsManager, alerts);
		}

		IFormatsWizardScenario IFactory.CreateOperationOverExistingFormatScenario(IWizardScenarioHost host)
		{
			return new OperationOverExistingFormatScenario(host, this);
		}

		FormatDeleteConfirmPage.IPresenter IFactory.CreateFormatDeleteConfirmPage(IWizardScenarioHost host)
		{
			return new FormatDeleteConfirmPage.Presenter(viewFactories.CreateFormatDeleteConfirmPageView(), host);
		}

		IFormatsWizardScenario IFactory.CreateDeleteFormatScenario(IWizardScenarioHost host)
		{
			return new DeleteFormatScenario(host, alerts, this);
		}

		IFormatsWizardScenario IFactory.CreateModifyRegexBasedFormatScenario(IWizardScenarioHost host)
		{
			return new ModifyRegexBasedFormatScenario(host, this);
		}

		IFormatsWizardScenario IFactory.CreateModifyXmlBasedFormatScenario(IWizardScenarioHost host)
		{
			return new ModifyXmlBasedFormatScenario(host, this);
		}

		IFormatsWizardScenario IFactory.CreateModifyJsonBasedFormatScenario(IWizardScenarioHost host)
		{
			return new ModifyJsonBasedFormatScenario(host, this);
		}

		RegexBasedFormatPage.IPresenter IFactory.CreateRegexBasedFormatPage(IWizardScenarioHost host)
		{
			return new RegexBasedFormatPage.Presenter(viewFactories.CreateRegexBasedFormatPageView(), 
				host, help, CreateTestParsing(), this);
		}

		EditSampleDialog.IPresenter IFactory.CreateEditSampleDialog ()
		{
			return new EditSampleDialog.Presenter(viewFactories.CreateEditSampleDialogView(),
				fileDialogs, alerts);
		}

		TestDialog.IPresenter IFactory.CreateTestDialog()
		{
			return new TestDialog.Presenter(viewFactories.CreateTestDialogView(),
				tempFilesManager, traceSourceFactory, regexFactory, logViewerPresenterFactory, synchronizationContext, fileSystem);
		}

		EditRegexDialog.IPresenter IFactory.CreateEditRegexDialog()
		{
			return new EditRegexDialog.Presenter(viewFactories.CreateEditRegexDialog(),
				help, alerts);
		}

		EditFieldsMapping.IPresenter IFactory.CreateEditFieldsMapping()
		{
			return new EditFieldsMapping.Presenter(viewFactories.CreateEditFieldsMappingDialog(),
				alerts, fileDialogs, fieldsProcessorFactory, help);
		}

		XmlBasedFormatPage.IPresenter IFactory.CreateXmlBasedFormatPage(IWizardScenarioHost host)
		{
			return new XmlBasedFormatPage.Presenter(viewFactories.CreateXmlBasedFormatPageView(), host,
				help, CreateTestParsing(), this);
		}

		JsonBasedFormatPage.IPresenter IFactory.CreateJsonBasedFormatPage(IWizardScenarioHost host)
		{
			return new JsonBasedFormatPage.Presenter(viewFactories.CreateJsonBasedFormatPageView(), host,
				help, CreateTestParsing(), this);
		}

		XsltEditorDialog.IPresenter IFactory.CreateXsltEditorDialog()
		{
			return new XsltEditorDialog.Presenter(viewFactories.CreateXsltEditorDialog(),
				help, alerts, CreateTestParsing());
		}

		JUSTEditorDialog.IPresenter IFactory.CreateJUSTEditorDialog()
		{
			return new JUSTEditorDialog.Presenter(viewFactories.CreateJUSTEditorDialog(),
				help, alerts, CreateTestParsing(), this);
		}

		private ITestParsing CreateTestParsing() => new CustomFormatPageUtils.TestParsing(
			alerts, tempFilesManager, traceSourceFactory, regexFactory, fieldsProcessorFactory, this);
	};
};