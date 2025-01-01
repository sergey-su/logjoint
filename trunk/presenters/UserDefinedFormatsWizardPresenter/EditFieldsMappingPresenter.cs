using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard.EditFieldsMapping
{
    internal class Presenter : IPresenter, IDisposable, IViewEvents
    {
        readonly IView dialog;
        readonly IAlertPopup alerts;
        readonly IFileDialogs fileDialogs;
        readonly FieldsProcessor.IFactory fieldsProcessorFactory;
        readonly Help.IPresenter help;
        static readonly string[] predefindOutputFields = { "Time", "Thread", "Body", "Severity" };
        int fieldIndex = 0;
        XmlNode grammarRoot;
        bool updateLock;
        string[] availableInputFields;
        List<Field> fieldsListBoxItems = new List<Field>();

        public Presenter(
            IView view,
            IAlertPopup alerts,
            IFileDialogs fileDialogs,
            FieldsProcessor.IFactory fieldsProcessorFactory,
            Help.IPresenter help
        )
        {
            this.dialog = view;
            this.alerts = alerts;
            this.fileDialogs = fileDialogs;
            this.fieldsProcessorFactory = fieldsProcessorFactory;
            this.help = help;
            this.dialog.SetEventsHandler(this);
        }

        void IDisposable.Dispose()
        {
            dialog.Dispose();
        }

        void IPresenter.ShowDialog(XmlNode root, string[] availableInputFields)
        {
            this.grammarRoot = root;
            this.availableInputFields = availableInputFields;

            InitAvailableFieldsList(availableInputFields);

            ReadMapping();
            UpdateView();

            dialog.Show();
        }

        Field Get()
        {
            return fieldsListBoxItems.ElementAtOrDefault(dialog.FieldsListBoxSelection);
        }

        Field Get(string name)
        {
            foreach (Field f in fieldsListBoxItems)
            {
                if (f.Name == name)
                    return f;
            }
            return null;
        }

        bool TrySelect(int idx)
        {
            if (idx >= 0 && idx < fieldsListBoxItems.Count)
            {
                dialog.FieldsListBoxSelection = idx;
                return true;
            }
            return false;
        }

        void InitAvailableFieldsList(string[] availableInputFields)
        {
            dialog.SetAvailableInputFieldsLinks(availableInputFields.Select(f =>
                                                                                Tuple.Create(f, (Action)(() => AvailableLinkClick(f)))).ToArray());
        }

        void AvailableLinkClick(string txt)
        {
            var fld = Get();
            if (fld == null)
                return;
            int selIdx = dialog.CodeTextBoxSelectionStart;
            var newCode = dialog.ReadControl(ControlId.CodeTextBox).Insert(selIdx, txt);
            dialog.ModifyControl(ControlId.CodeTextBox, text: newCode);
            dialog.ModifyCodeTextBoxSelection(selIdx + txt.Length, 0);
            fld.Code = newCode;
        }

        void ReadMapping()
        {
            foreach (XmlElement e in grammarRoot.SelectNodes("fields-config/field[@name]"))
            {
                Field f = new Field(e.GetAttribute("name"));
                if (e.GetAttribute("code-type") == "function")
                    f.CodeType = FieldCodeType.Function;
                f.Code = e.InnerText;
                fieldsListBoxItems.Add(f);
                dialog.AddFieldsListBoxItem(f.ToString());
            }
        }

        XmlNode WriteMappingInternal(XmlNode grammarRoot)
        {
            XmlNode cfgNode = grammarRoot.SelectSingleNode("fields-config");
            if (cfgNode != null)
                cfgNode.RemoveAll();
            else
                cfgNode = grammarRoot.AppendChild(grammarRoot.OwnerDocument.CreateElement("fields-config"));
            foreach (Field f in fieldsListBoxItems)
            {
                XmlElement e = grammarRoot.OwnerDocument.CreateElement("field");
                e.SetAttribute("name", f.Name);
                if (f.CodeType == FieldCodeType.Function)
                    e.SetAttribute("code-type", "function");
                e.ReplaceValueWithCData(f.Code);
                cfgNode.AppendChild(e);
            }
            return cfgNode;
        }

        void WriteMapping()
        {
            WriteMappingInternal(this.grammarRoot);
        }

        public enum FieldCodeType
        {
            Expression,
            Function
        };

        public class Field
        {
            public string Name;
            public FieldCodeType CodeType = FieldCodeType.Expression;
            public string Code = "";

            public Field(string name)
            {
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        };

        void UpdateView()
        {
            if (updateLock)
                return;
            Field f = Get();
            dialog.ModifyControl(ControlId.RemoveFieldButton, enabled: f != null);
            dialog.ModifyControl(ControlId.NameComboBox, enabled: f != null);
            dialog.ModifyControl(ControlId.CodeTypeComboBox, enabled: f != null);
            dialog.ModifyControl(ControlId.CodeTextBox, enabled: f != null);
            dialog.ModifyControl(ControlId.AvailableInputFieldsContainer, enabled: f != null);
            if (f != null)
            {
                dialog.ModifyControl(ControlId.NameComboBox, text: f.Name);
                dialog.CodeTypeComboBoxSelectedIndex = (int)f.CodeType;
                dialog.ModifyControl(ControlId.CodeTextBox, text: f.Code);
                dialog.SetControlOptions(ControlId.NameComboBox,
                                             predefindOutputFields.Where(s => Get(s) == null).ToArray());
            }
            else
            {
                dialog.ModifyControl(ControlId.NameComboBox, text: "");
                dialog.CodeTypeComboBoxSelectedIndex = -1;
                dialog.ModifyControl(ControlId.CodeTextBox, text: "");
            }
        }

        string ValidateInput()
        {
            Dictionary<string, bool> fldMap = new Dictionary<string, bool>();
            foreach (Field f in fieldsListBoxItems)
            {
                if (f.Name == "")
                    return "One of the fields has empty name. The name can not be empty.";
                if (fldMap.ContainsKey(f.Name))
                    return "Field name duplicate: " + f.Name;
                fldMap.Add(f.Name, true);
            }
            if (!fldMap.ContainsKey("Time"))
                return "There must create a field with name 'Time'. This field is required.";
            return null;
        }

        void HandleProcessorError(BadUserCodeException exception, bool alwaysFallToAdvancedMode)
        {
            if (!alwaysFallToAdvancedMode && exception.BadField != null)
            {
                Field field = Get(exception.BadField.FieldName);
                if (field != null)
                {
                    dialog.FieldsListBoxSelection = fieldsListBoxItems.IndexOf(field);
                    dialog.ModifyCodeTextBoxSelection(exception.BadField.ErrorPosition, 1);

                    alerts.ShowPopup("Error", exception.ErrorMessage, AlertFlags.Ok | AlertFlags.WarningIcon);
                    return;
                }
            }

            if (alerts.ShowPopup(
                "Error",
                "LogJoint tried to combine your code into a class that would create LogJoin messages out of the regex captures. " +
                "The combined code can not be compiled and the errors are outside your code. " +
                "Although most likely the errors are caused by mistakes in the code you provided. " +
                "It is recommended to doublecheck fields code.\n\n" +
                "Error message: " + exception.ErrorMessage + "\n\n" +
                "LogJoint can save combined code and detailed error messages into a file so you could analize them. " +
                "Do you want to save this file?",
                AlertFlags.YesNoCancel | AlertFlags.WarningIcon) == AlertFlags.Yes)
            {
                string fileName;
                if ((fileName = fileDialogs.SaveFileDialog(new SaveFileDialogParams()
                {
                    SuggestedFileName = "code.cs",
                })) != null)
                {
                    try
                    {
                        using (TextWriter fs = new StreamWriter(fileName, false, Encoding.UTF8))
                        {
                            fs.WriteLine(exception.FullCode);
                            fs.WriteLine();
                            fs.WriteLine("Compilation errors:");
                            fs.Write(exception.AllErrors);
                        }
                    }
                    catch (Exception e)
                    {
                        alerts.ShowPopup("Error", "Failed to save file. " + e.Message,
                                                   AlertFlags.WarningIcon | AlertFlags.Ok);
                    }
                }
            }
        }

        void IViewEvents.OnAddFieldButtonClicked()
        {
            Field f = new Field(string.Format("Field {0}", ++fieldIndex));
            fieldsListBoxItems.Add(f);
            dialog.AddFieldsListBoxItem(f.ToString());
            dialog.FieldsListBoxSelection = fieldsListBoxItems.Count - 1;
        }

        void IViewEvents.OnSelectedFieldChanged()
        {
            UpdateView();
        }

        void IViewEvents.OnRemoveFieldButtonClicked()
        {
            int idx = dialog.FieldsListBoxSelection;
            if (idx >= 0)
            {
                dialog.RemoveFieldsListBoxItem(idx);
                fieldsListBoxItems.RemoveAt(idx);
            }
            if (!TrySelect(idx) && !TrySelect(idx - 1))
                UpdateView();
        }

        void IViewEvents.OnNameComboBoxTextChanged()
        {
            Field f = Get();
            if (f == null)
                return;
            if (f.Name == dialog.ReadControl(ControlId.NameComboBox))
                return;
            f.Name = dialog.ReadControl(ControlId.NameComboBox);
            updateLock = true;
            try
            {
                dialog.ChangeFieldsListBoxItem(dialog.FieldsListBoxSelection, f.ToString());
            }
            finally
            {
                updateLock = false;
            }
        }

        void IViewEvents.OnCodeTypeSelectedIndexChanged()
        {
            if (Get() != null)
                Get().CodeType = (FieldCodeType)dialog.CodeTypeComboBoxSelectedIndex;
        }

        void IViewEvents.OnCodeTextBoxChanged()
        {
            if (Get() != null)
                Get().Code = dialog.ReadControl(ControlId.CodeTextBox);
        }

        void IViewEvents.OnOkClicked()
        {
            string msg = ValidateInput();
            if (msg != null)
            {
                alerts.ShowPopup("Validation", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
                return;
            }
            WriteMapping();
            dialog.Close();
        }

        void IViewEvents.OnCancelClicked()
        {
            dialog.Close();
        }

        async void IViewEvents.OnTestClicked(bool advancedModeModifierIsHeld)
        {
            XmlDocument tmp = new XmlDocument();
            XmlNode root = tmp.AppendChild(tmp.CreateElement("root"));
            XmlNode mapping = WriteMappingInternal(root);

            XDocument tmpXDoc = XDocument.Parse(tmp.OuterXml);

            FieldsProcessor.IInitializationParams tmpProcessorParams = fieldsProcessorFactory.CreateInitializationParams(
                tmpXDoc.Element("root").Element("fields-config"), false);
            try
            {
                var tmpProcessor = await fieldsProcessorFactory.CreateProcessor(
                    tmpProcessorParams, availableInputFields, null, LJTraceSource.EmptyTracer);
                tmpProcessor.Reset();
                alerts.ShowPopup("Test", "Code compiled OK", AlertFlags.Ok);
            }
            catch (BadUserCodeException e)
            {
                HandleProcessorError(e, advancedModeModifierIsHeld);
            }
        }

        void IViewEvents.OnHelpLinkClicked()
        {
            help.ShowHelp("FieldsMapping.htm");
        }
    };
};