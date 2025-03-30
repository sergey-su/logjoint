
using LogJoint.Drawing;
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
        void SetCurrentFormat(IUserDefinedFactory udf);
    };


    public interface IWizardPagePresenter
    {
        bool ExitPage(bool movingForward);
        object ViewObject { get; }
    };

    public interface IFactory
    {
        IView CreateWizardView();

        IFormatsWizardScenario CreateRootScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateImportLog4NetScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateImportNLogScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateOperationOverExistingFormatScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateDeleteFormatScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateModifyRegexBasedFormatScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateModifyXmlBasedFormatScenario(IWizardScenarioHost host);
        IFormatsWizardScenario CreateModifyJsonBasedFormatScenario(IWizardScenarioHost host);

        ChooseOperationPage.IPresenter CreateChooseOperationPage(IWizardScenarioHost host);
        ImportLog4NetPage.IPresenter CreateImportLog4NetPage(IWizardScenarioHost host);
        FormatIdentityPage.IPresenter CreateFormatIdentityPage(IWizardScenarioHost host, bool newFormatMode);
        FormatAdditionalOptionsPage.IPresenter CreateFormatAdditionalOptionsPage(IWizardScenarioHost host);
        SaveFormatPage.IPresenter CreateSaveFormatPage(IWizardScenarioHost host, bool newFormatMode);
        NLogGenerationLogPage.IPresenter CreateNLogGenerationLogPage(IWizardScenarioHost host);
        ImportNLogPage.IPresenter CreateImportNLogPage(IWizardScenarioHost host);
        ChooseExistingFormatPage.IPresenter CreateChooseExistingFormatPage(IWizardScenarioHost host);
        FormatDeleteConfirmPage.IPresenter CreateFormatDeleteConfirmPage(IWizardScenarioHost host);
        RegexBasedFormatPage.IPresenter CreateRegexBasedFormatPage(IWizardScenarioHost host);
        EditSampleDialog.IPresenter CreateEditSampleDialog();
        TestDialog.IPresenter CreateTestDialog();
        EditRegexDialog.IPresenter CreateEditRegexDialog();
        EditFieldsMapping.IPresenter CreateEditFieldsMapping();
        XmlBasedFormatPage.IPresenter CreateXmlBasedFormatPage(IWizardScenarioHost host);
        JsonBasedFormatPage.IPresenter CreateJsonBasedFormatPage(IWizardScenarioHost host);
        XsltEditorDialog.IPresenter CreateXsltEditorDialog();
        JUSTEditorDialog.IPresenter CreateJUSTEditorDialog();
    };

    namespace ChooseOperationPage
    {
        public interface IPresenter : IWizardPagePresenter
        {
            ControlId SelectedControl { get; }
        };

        public enum ControlId
        {
            None,
            ImportLog4NetButton,
            ImportNLogButton,
            ChangeFormatButton,
            NewREBasedButton,
            NewXMLBasedButton,
            NewJsonBasedButton
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
            void SetViewModel(IViewModel eventsHandler);
            void SetPatternsListBoxItems(string[] value);
            int[] GetPatternsListBoxSelection();
            void SetEncodingComboBoxItems(string[] items);
            int EncodingComboBoxSelection { get; set; }
            bool EnableDejitterCheckBoxChecked { get; set; }
            void EnableControls(bool addExtensionButton, bool removeExtensionButton);
            string ExtensionTextBoxValue { get; set; }
        };

        public interface IViewModel
        {
            void OnExtensionTextBoxChanged();
            void OnExtensionsListBoxSelectionChanged();
            void OnAddExtensionClicked();
            void OnDelExtensionClicked();
            void OnEnableDejitterCheckBoxClicked();
            void OnDejitterHelpLinkClicked();
            LabeledStepperPresenter.IViewModel BufferStepper { get; }
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
            ISelectedLayout GetSelectedLayout();
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

        public interface ISelectedLayout
        {
        };

        public interface ISimpleLayout : ISelectedLayout
        {
            string Value { get; }
        };

        public interface ICSVLayout : ISelectedLayout
        {
            NLog.CsvParams Params { get; }
        };

        public interface IJsonLayout : ISelectedLayout
        {
            NLog.JsonParams Params { get; }
        };
    };

    namespace NLogGenerationLogPage
    {
        public interface IPresenter : IWizardPagePresenter
        {
            void UpdateView(ImportNLogPage.ISelectedLayout layout, NLog.ImportLog log);
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

    public interface ISampleLogAccess
    {
        string SampleLog { get; set; }
    };

    namespace EditSampleDialog
    {
        public interface IView : IDisposable
        {
            void SetEventsHandler(IViewEvents eventsHandler);
            string SampleLogTextBoxValue { get; set; }
            void Show();
            void Close();
        };

        public interface IViewEvents
        {
            void OnCloseButtonClicked(bool accepted);
            void OnLoadSampleButtonClicked();
        };

        public interface IPresenter : IDisposable
        {
            void ShowDialog(ISampleLogAccess sampleLog);
        };
    };

    namespace TestDialog
    {
        public enum TestOutcome
        {
            None,
            Success,
            Failure
        }

        public interface IView : IDisposable
        {
            void SetEventsHandler(IViewEvents eventsHandler);
            void Show();
            void Close();
            void SetData(string message, TestOutcome testOutcome);
            LogViewer.IView LogViewer { get; }
        };

        public interface IViewEvents
        {
            void OnCloseButtonClicked();
        };

        public interface IPresenter : IDisposable
        {
            bool ShowDialog(ILogProviderFactory sampleLogFactory, IConnectionParams sampleLogConnectionParams);
        };
    };

    namespace EditRegexDialog
    {
        public interface IPresenter : IDisposable
        {
            void ShowDialog(XmlNode formatRootNode, bool headerReMode, ISampleLogAccess sampleLog);
        };

        public enum ControlId
        {
            None,
            Dialog,
            RegExTextBox,
            SampleLogTextBox,
            ReHelpLabel,
            EmptyReLabel,
            MatchesCountLabel,
            PerfValueLabel,
            LegendList,
            LegendLabel
        };

        public class CapturesListBoxItem
        {
            public string Text { get; internal set; }
            public Color Color { get; internal set; }
        };

        public struct TextPatch
        {
            public int RangeBegin { get; internal set; }
            public int RangeEnd { get; internal set; }
            public Color? BackColor { get; internal set; }
            public Color? ForeColor { get; internal set; }
            public bool? Bold { get; internal set; }
        };

        public interface IView : IDisposable
        {
            void SetEventsHandler(IViewEvents events);
            void Show();
            void Close();
            string ReadControl(ControlId ctrl);
            void WriteControl(ControlId ctrl, string value);
            void ClearCapturesListBox();
            void EnableControl(ControlId ctrl, bool enable);
            void SetControlVisibility(ControlId ctrl, bool value);
            void AddCapturesListBoxItem(CapturesListBoxItem item);
            void ResetSelection(ControlId ctrl);
            void PatchLogSample(TextPatch p);
        };

        public interface IViewEvents
        {
            void OnExecRegexButtonClicked();
            void OnExecRegexShortcut();
            void OnSampleEditTextChanged();
            void OnCloseButtonClicked(bool accepted);
            void OnConceptsLinkClicked();
            void OnRegexHelpLinkClicked();
            void OnRegExTextBoxTextChanged();
        };
    };

    namespace EditFieldsMapping
    {
        public interface IPresenter : IDisposable
        {
            void ShowDialog(XmlNode root, string[] availableInputFields);
        };

        public enum ControlId
        {
            None,
            RemoveFieldButton,
            NameComboBox,
            CodeTypeComboBox,
            CodeTextBox,
            AvailableInputFieldsContainer,
        };

        public interface IView : IDisposable
        {
            void SetEventsHandler(IViewEvents events);
            void Show();
            void Close();
            void AddFieldsListBoxItem(string text);
            void RemoveFieldsListBoxItem(int idx);
            void ChangeFieldsListBoxItem(int idx, string value);
            int FieldsListBoxSelection { get; set; }
            void ModifyControl(ControlId id, string text = null, bool? enabled = null);
            int CodeTypeComboBoxSelectedIndex { get; set; }
            void SetControlOptions(ControlId id, string[] options);
            void SetAvailableInputFieldsLinks(Tuple<string, Action>[] links);
            string ReadControl(ControlId id);
            int CodeTextBoxSelectionStart { get; }
            void ModifyCodeTextBoxSelection(int start, int len);
        };

        public interface IViewEvents
        {
            void OnAddFieldButtonClicked();
            void OnSelectedFieldChanged();
            void OnRemoveFieldButtonClicked();
            void OnNameComboBoxTextChanged();
            void OnCodeTypeSelectedIndexChanged();
            void OnCodeTextBoxChanged();
            void OnOkClicked();
            void OnCancelClicked();
            void OnTestClicked(bool advancedModeModifierIsHeld);
            void OnHelpLinkClicked();
        };
    };

    namespace RegexBasedFormatPage
    {
        public interface IPresenter : IWizardPagePresenter
        {
            void SetFormatRoot(XmlElement root);
        };

        public interface IView
        {
            void SetEventsHandler(IViewEvents eventsHandler);
            void SetLabelProps(ControlId labelId, string text, Color color);
        };

        public enum ControlId
        {
            None,
            HeaderReStatusLabel,
            BodyReStatusLabel,
            FieldsMappingLabel,
            TestStatusLabel,
            SampleLogStatusLabel
        };

        public interface IViewEvents
        {
            void OnSelectSampleButtonClicked();
            void OnTestButtonClicked();
            void OnChangeHeaderReButtonClicked();
            void OnChangeBodyReButtonClicked();
            void OnConceptsLinkClicked();
            void OnChangeFieldsMappingButtonClick();
        };
    };

    namespace CustomTransformBasedFormatPage
    {
        public enum ControlId
        {
            None,
            PageTitleLabel,
            ConceptsLabel,
            SampleLogLabel,
            SampleLogStatusLabel,
            HeaderReLabel,
            HeaderReStatusLabel,
            TransformLabel,
            TransformStatusLabel,
            TestLabel,
            TestStatusLabel,
        };

        public interface IView
        {
            void SetEventsHandler(IViewEvents eventsHandler);
            void SetLabelProps(ControlId labelId, string text, Color? color);
        };

        public interface IViewEvents
        {
            void OnSelectSampleButtonClicked();
            void OnTestButtonClicked();
            void OnChangeHeaderReButtonClicked();
            void OnChangeTransformButtonClicked();
            void OnConceptsLinkClicked();
        };
    }

    namespace XmlBasedFormatPage
    {
        public interface IPresenter : IWizardPagePresenter
        {
            void SetFormatRoot(XmlElement root);
        };
    }

    namespace JsonBasedFormatPage
    {
        public interface IPresenter : IWizardPagePresenter
        {
            void SetFormatRoot(XmlElement root);
        };
    }

    namespace CustomCodeEditorDialog
    {
        public interface IView : IDisposable
        {
            void SetEventsHandler(IViewEvents events);
            void Show();
            void Close();
            void InitStaticControls(string dialogCaption, string titleValue, string helpLinkValue);
            string CodeTextBoxValue { get; set; }
        };

        public interface IViewEvents
        {
            void OnOkClicked();
            void OnCancelClicked();
            void OnHelpLinkClicked();
            void OnTestButtonClicked();
        };
    };

    namespace XsltEditorDialog
    {
        public interface IPresenter : IDisposable
        {
            void ShowDialog(XmlNode root, ISampleLogAccess sampleLogAccess);
        };
    };

    namespace JUSTEditorDialog
    {
        public interface IPresenter : IDisposable
        {
            void ShowDialog(XmlNode root, ISampleLogAccess sampleLogAccess);
        };
    };
};