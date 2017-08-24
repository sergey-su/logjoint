using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class ObjectsFactory : IObjectFactory
	{
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly ILogProviderFactoryRegistry registry;
		readonly IFormatDefinitionsRepository repo;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly LogViewer.IPresenterFactory logViewerPresenterFactory;
		readonly ViewFactories viewFactories;

		public struct ViewFactories
		{
			public Func<IView> CreateFormatsWizardView;
			public Func<ChooseOperationPage.IView> CreateChooseOperationPageView;
			public Func<ImportLog4NetPage.IView> CreateImportLog4NetPagePageView;
			public Func<FormatIdentityPage.IView> CreateFormatIdentityPageView;
			public Func<FormatAdditionalOptionsPage.IView> CreateFormatAdditionalOptionsPage;
			public Func<SaveFormatPage.IView> CreateSaveFormatPageView;
			public Func<NLogGenerationLogPage.IView> CreateNLogGenerationLogPageView;
			public Func<ImportNLogPage.IView> CreateImportNLogPage;
			public Func<ChooseExistingFormatPage.IView> CreateChooseExistingFormatPageView;
			public Func<FormatDeleteConfirmPage.IView> CreateFormatDeleteConfirmPageView;
			public Func<RegexBasedFormatPage.IView> CreateRegexBasedFormatPageView;
			public Func<EditSampleDialog.IView> CreateEditSampleDialogView;
			public Func<TestDialog.IView> CreateTestDialogView;
			public Func<EditRegexDialog.IView> CreateEditRegexDialog;
			public Func<EditFieldsMapping.IView> CreateEditFieldsMappingDialog;
			public Func<XmlBasedFormatPage.IView> CreateXmlBasedFormatPageView;
			public Func<XsltEditorDialog.IView> CreateXsltEditorDialog;
		};

		public ObjectsFactory(
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			Help.IPresenter help,
			ILogProviderFactoryRegistry registry,
			IFormatDefinitionsRepository repo,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ITempFilesManager tempFilesManager,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			ViewFactories viewFactories
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
		}

		IView IObjectFactory.CreateWizardView()
		{
			return viewFactories.CreateFormatsWizardView();
		}

		ChooseOperationPage.IPresenter IObjectFactory.CreateChooseOperationPage(IWizardScenarioHost host)
		{
			return new ChooseOperationPage.Presenter(viewFactories.CreateChooseOperationPageView(), host);
		}

		IFormatsWizardScenario IObjectFactory.CreateRootScenario(IWizardScenarioHost host)
		{
			return new RootScenario(host, this);
		}

		IFormatsWizardScenario IObjectFactory.CreateImportLog4NetScenario(IWizardScenarioHost host)
		{
			return new ImportLog4NetScenario(host, this);
		}

		ImportLog4NetPage.IPresenter IObjectFactory.CreateImportLog4NetPage(IWizardScenarioHost host)
		{
			return new ImportLog4NetPage.Presenter(viewFactories.CreateImportLog4NetPagePageView(), host, alerts, fileDialogs);
		}

		FormatIdentityPage.IPresenter IObjectFactory.CreateFormatIdentityPage(IWizardScenarioHost host, bool newFormatMode)
		{
			return new FormatIdentityPage.Presenter(viewFactories.CreateFormatIdentityPageView(), host, alerts, registry, newFormatMode);
		}

		FormatAdditionalOptionsPage.IPresenter IObjectFactory.CreateFormatAdditionalOptionsPage(IWizardScenarioHost host)
		{
			return new FormatAdditionalOptionsPage.Presenter(viewFactories.CreateFormatAdditionalOptionsPage(), host, help);
		}

		SaveFormatPage.IPresenter IObjectFactory.CreateSaveFormatPage(IWizardScenarioHost host, bool newFormatMode)
		{
			return new SaveFormatPage.Presenter(viewFactories.CreateSaveFormatPageView(), host, alerts, repo, newFormatMode);
		}

		NLogGenerationLogPage.IPresenter IObjectFactory.CreateNLogGenerationLogPage(IWizardScenarioHost host)
		{
			return new NLogGenerationLogPage.Presenter(viewFactories.CreateNLogGenerationLogPageView(), host);
		}

		ImportNLogPage.IPresenter IObjectFactory.CreateImportNLogPage(IWizardScenarioHost host)
		{
			return new ImportNLogPage.Presenter(viewFactories.CreateImportNLogPage(), host, alerts, fileDialogs);
		}

		IFormatsWizardScenario IObjectFactory.CreateImportNLogScenario(IWizardScenarioHost host)
		{
			return new ImportNLogScenario(host, this, alerts);
		}

		ChooseExistingFormatPage.IPresenter IObjectFactory.CreateChooseExistingFormatPage(IWizardScenarioHost host)
		{
			return new ChooseExistingFormatPage.Presenter(viewFactories.CreateChooseExistingFormatPageView(), host, userDefinedFormatsManager, alerts);
		}

		IFormatsWizardScenario IObjectFactory.CreateOperationOverExistingFormatScenario(IWizardScenarioHost host)
		{
			return new OperationOverExistingFormatScenario(host, this);
		}

		FormatDeleteConfirmPage.IPresenter IObjectFactory.CreateFormatDeleteConfirmPage(IWizardScenarioHost host)
		{
			return new FormatDeleteConfirmPage.Presenter(viewFactories.CreateFormatDeleteConfirmPageView(), host);
		}

		IFormatsWizardScenario IObjectFactory.CreateDeleteFormatScenario(IWizardScenarioHost host)
		{
			return new DeleteFormatScenario(host, alerts, this);
		}

		IFormatsWizardScenario IObjectFactory.CreateModifyRegexBasedFormatScenario(IWizardScenarioHost host)
		{
			return new ModifyRegexBasedFormatScenario(host, this);
		}

		IFormatsWizardScenario IObjectFactory.CreateModifyXmlBasedFormatScenario(IWizardScenarioHost host)
		{
			return new ModifyXmlBasedFormatScenario(host, this);
		}

		RegexBasedFormatPage.IPresenter IObjectFactory.CreateRegexBasedFormatPage(IWizardScenarioHost host)
		{
			return new RegexBasedFormatPage.Presenter(viewFactories.CreateRegexBasedFormatPageView(), 
				host, help, tempFilesManager, alerts, this);
		}

		EditSampleDialog.IPresenter IObjectFactory.CreateEditSampleDialog ()
		{
			return new EditSampleDialog.Presenter(viewFactories.CreateEditSampleDialogView(),
				fileDialogs, alerts);
		}

		TestDialog.IPresenter IObjectFactory.CreateTestDialog()
		{
			return new TestDialog.Presenter(viewFactories.CreateTestDialogView(),
				tempFilesManager, logViewerPresenterFactory);
		}

		EditRegexDialog.IPresenter IObjectFactory.CreateEditRegexDialog()
		{
			return new EditRegexDialog.Presenter(viewFactories.CreateEditRegexDialog(),
				help, alerts);
		}

		EditFieldsMapping.IPresenter IObjectFactory.CreateEditFieldsMapping()
		{
			return new EditFieldsMapping.Presenter(viewFactories.CreateEditFieldsMappingDialog(),
				alerts, fileDialogs, tempFilesManager, help);
		}

		XmlBasedFormatPage.IPresenter IObjectFactory.CreateXmlBasedFormatPage(IWizardScenarioHost host)
		{
			return new XmlBasedFormatPage.Presenter(viewFactories.CreateXmlBasedFormatPageView(), host,
				help, tempFilesManager, alerts, this);			
		}

		XsltEditorDialog.IPresenter IObjectFactory.CreateXsltEditorDialog()
		{
			return new XsltEditorDialog.Presenter(viewFactories.CreateXsltEditorDialog(),
				help, alerts, tempFilesManager, this);			
		}
	};
};