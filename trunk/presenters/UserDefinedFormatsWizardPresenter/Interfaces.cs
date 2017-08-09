
using System;
using System.Collections.Generic;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public interface IPresenter
	{
		void ShowDialog();
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		void ShowDialog();
		void CloseDialog();
		void SetControls(string backText, bool backEnabled, string nextText, bool nextEnabled);
		void HidePage(object viewObject);
		void ShowPage(object viewObject);
	};

	public interface IViewEvents
	{
		void OnBackClicked();
		void OnNextClicked();
		void OnCloseClicked();
	};

	public interface IWizardScenarioHost
	{
		void Next();
		void Back();
		void Finish();
	};

	[Flags]
	public enum WizardScenarioFlag
	{
		None = 0,
		BackIsActive = 1,
		NextIsActive = 2,
		NextIsFinish = 4
	};

	public interface IFormatsWizardScenario
	{
		bool Next();
		bool Prev();
		IWizardPagePresenter Current { get; }
		WizardScenarioFlag Flags { get; }
	};

	public interface IWizardPagePresenter
	{
		bool ExitPage(bool movingForward);
		object ViewObject { get; }
	};

	public interface IObjectFactory
	{
		IView CreateWizardView();

		IFormatsWizardScenario CreateRootScenario(IWizardScenarioHost host);
		IFormatsWizardScenario CreateImportLog4NetScenario(IWizardScenarioHost host);
		IFormatsWizardScenario CreateImportNLogScenario(IWizardScenarioHost host);
		IFormatsWizardScenario CreateOperationOverExistingFormatScenario(IWizardScenarioHost host);
		IFormatsWizardScenario CreateDeleteFormatScenario(IWizardScenarioHost host, IUserDefinedFactory udf);

		ChooseOperationPage.IPresenter CreateChooseOperationPage(IWizardScenarioHost host);
		ImportLog4NetPage.IPresenter CreateImportLog4NetPage(IWizardScenarioHost host);
		FormatIdentityPage.IPresenter CreateFormatIdentityPage(IWizardScenarioHost host, bool newFormatMode);
		FormatAdditionalOptionsPage.IPresenter CreateFormatAdditionalOptionsPage(IWizardScenarioHost host);
		SaveFormatPage.IPresenter CreateSaveFormatPage(IWizardScenarioHost host, bool newFormatMode);
		NLogGenerationLogPage.IPresenter CreateNLogGenerationLogPage(IWizardScenarioHost host);
		ImportNLogPage.IPresenter CreateImportNLogPage(IWizardScenarioHost host);
		ChooseExistingFormatPage.IPresenter CreateChooseExistingFormatPage(IWizardScenarioHost host);
		FormatDeleteConfirmPage.IPresenter CreateFormatDeleteConfirmPage(IWizardScenarioHost host);
	};

	namespace ChooseOperationPage
	{
		public interface IPresenter: IWizardPagePresenter
		{
			ControlId SelectedControl { get; }
		};

		public enum ControlId
		{
			None,
			ImportLog4NetButton,
			ImportNLogButton,
			ChangeFormatButton,
			NewREBasedButton
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			ControlId SelectedControl { get; }
		};

		public interface IViewEvents
		{
			void OnOptionDblClicked();
		};
	};

	namespace ImportLog4NetPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			bool ValidateInput();
			bool GenerateGrammar(XmlElement e);
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			string PatternTextBoxValue { get; set; }
			void SetAvailablePatternsListItems(string[] value);
			void SetConfigFileTextBoxValue(string value);
		};

		public interface IViewEvents
		{
			void OnOpenConfigButtonClicked();
			void OnSelectedAvailablePatternChanged(int idx);
			void OnSelectedAvailablePatternDoubleClicked();
		};
	};

	namespace FormatIdentityPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			void SetFormatRoot(XmlNode formatRoot);
			string GetDefaultFileNameBasis();
		};

		public enum ControlId
		{
			None,
			HeaderLabel,
			CompanyNameEdit,
			FormatNameEdit,
			DescriptionEdit
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			string this[ControlId id] { get; set; }
			void SetFocus(ControlId id);
		};

		public interface IViewEvents
		{
		};
	};

	namespace FormatAdditionalOptionsPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			void SetFormatRoot(XmlNode formatRoot);
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			void InitDejitterBufferSizeGauge(int[] values, int initialValue);
			void SetPatternsListBoxItems(string[] value);
			int[] GetPatternsListBoxSelection();
			void SetEncodingComboBoxItems(string[] items);
			int EncodingComboBoxSelection { get; set; }
			bool EnableDejitterCheckBoxChecked { get; set; }
			int DejitterBufferSizeGaugeValue { get; set; }
			void EnableControls(bool addExtensionButton, bool removeExtensionButton, bool dejitterBufferSizeGauge);
			string ExtensionTextBoxValue { get; set; }
		};

		public interface IViewEvents
		{
			void OnExtensionTextBoxChanged();
			void OnExtensionsListBoxSelectionChanged();
			void OnAddExtensionClicked();
			void OnDelExtensionClicked();
			void OnEnableDejitterCheckBoxClicked();
			void OnDejitterHelpLinkClicked();
		};
	};

	namespace SaveFormatPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			void SetDocument(XmlDocument doc);
			string FileNameBasis { get; set; }
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			string FileNameBasisTextBoxValue { get; set; }
			string FileNameTextBoxValue { get; set; }
		};

		public interface IViewEvents
		{
			void OnFileNameBasisTextBoxChanged();
		};
	};

	namespace ImportNLogPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			bool ValidateInput();
			string Pattern { get; }
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			void SetAvailablePatternsListBoxItems(string[] values);
			string PatternTextBoxValue { get; set; }
			string ConfigFileTextBoxValue { get; set; }
		};

		public interface IViewEvents
		{
			void OnOpenConfigButtonClicked();
			void OnSelectedAvailablePatternDoubleClicked();
			void OnSelectedAvailablePatternChanged(int idx);
		};
	};

	namespace NLogGenerationLogPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			void UpdateView(string pattern, NLog.ImportLog log);
		};

		public enum IconType
		{
			None,
			ErrorIcon,
			WarningIcon,
			NeutralIcon,
		};

		public class MessagesListItem
		{
			public string Text { get; internal set; }
			public IconType Icon { get; internal set; }
			public List<Tuple<int, int, Action>> Links { get; internal set; }
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			void Update(
				string layoutTextboxValue,
				string headerLabelValue,
				IconType headerIcon,
				MessagesListItem[] messagesList
			);
			void SelectLayoutTextRange(int idx, int len);
		};

		public interface IViewEvents
		{
		};
	};

	namespace ChooseExistingFormatPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			bool ValidateInput();
			ControlId SelectedOption { get; }
			IUserDefinedFactory SelectedFormat { get; }
		};

		public enum ControlId
		{
			None,
			Delete,
			Change
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			ControlId SelectedOption { get; }
			void SetFormatsListBoxItems(string[] items);
			int SelectedFormatsListBoxItem { get; }
		};

		public interface IViewEvents
		{
			void OnControlDblClicked();
		};
	};

	namespace FormatDeleteConfirmPage
	{
		public interface IPresenter : IWizardPagePresenter
		{
			void UpdateView(IUserDefinedFactory format);
		};

		public interface IView
		{
			void SetEventsHandler(IViewEvents eventsHandler);
			void Update(string messageLabelText, string descriptionTextBoxValue, string fileNameTextBoxValue, string dateTextBoxValue);
		};

		public interface IViewEvents
		{
		};
	};
};