using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;
using System.Threading;

namespace LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage
{
	internal class Presenter : IPresenter, IViewEvents, ISampleLogAccess
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly IObjectFactory objectsFactory;
		XmlNode formatRoot;
		XmlNode reGrammarRoot;
		bool testOk;
		static readonly string[] parameterStatusStrings = new string[] { "Not set", "OK" };
		static readonly string[] testStatusStrings = new string[] { "", "Passed" };
		static readonly string sampleLogNodeName = "sample-log";
		string sampleLogCache = null;

		public Presenter(
			IView view, 
			IWizardScenarioHost host,
			Help.IPresenter help, 
			ITempFilesManager tempFilesManager,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			IObjectFactory objectsFactory
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.help = help;
			this.tempFilesManager = tempFilesManager;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.objectsFactory = objectsFactory;
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			return true;
		}

		object IWizardPagePresenter.ViewObject => view;

		void IPresenter.SetFormatRoot(XmlElement formatRoot)
		{
			this.sampleLogCache = null;
			this.formatRoot = formatRoot;
			this.reGrammarRoot = formatRoot.SelectSingleNode("regular-grammar");
			if (this.reGrammarRoot == null)
				this.reGrammarRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("regular-grammar"));
			UpdateView();
		}

		void InitStatusLabel(ControlId label, bool statusOk, string[] strings)
		{
			view.SetLabelProps(
				label,
				statusOk ? strings[1] : strings[0],
				statusOk ? new ModelColor(0xFF008000) : new ModelColor(0xFF000000)
			);
		}

		public void UpdateView()
		{
			InitStatusLabel(ControlId.HeaderReStatusLabel, reGrammarRoot.SelectSingleNode("head-re[text()!='']") != null, parameterStatusStrings);
			InitStatusLabel(ControlId.BodyReStatusLabel, reGrammarRoot.SelectSingleNode("body-re[text()!='']") != null, parameterStatusStrings);
			InitStatusLabel(ControlId.FieldsMappingLabel, reGrammarRoot.SelectSingleNode("fields-config[field[@name='Time']]") != null, parameterStatusStrings);
			InitStatusLabel(ControlId.TestStatusLabel, testOk, testStatusStrings);
			InitStatusLabel(ControlId.SampleLogStatusLabel, GetSampleLog() != "", parameterStatusStrings);
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

		void IViewEvents.OnSelectSampleButtonClicked()
		{
			using (var editSampleDialog = objectsFactory.CreateEditSampleDialog())
			{
				editSampleDialog.ShowDialog(this);
			}
			UpdateView();
		}

		void IViewEvents.OnTestButtonClicked()
		{
			if (GetSampleLog() == "")
			{
				alerts.ShowPopup("", "Provide a sample log first", AlertFlags.Ok | AlertFlags.WarningIcon);
				return;
			}

			string tmpLog = tempFilesManager.GenerateNewName();
			try
			{
				XDocument clonedFormatXmlDocument = XDocument.Parse(formatRoot.OuterXml);

				UserDefinedFactoryParams createParams;
				createParams.Entry = null;
				createParams.RootNode = clonedFormatXmlDocument.Element("format");
				createParams.FormatSpecificNode = createParams.RootNode.Element("regular-grammar");
				createParams.FactoryRegistry = null;
				createParams.TempFilesManager = tempFilesManager;

				// Temporary sample file is always written in Unicode wo BOM: we don't test encoding detection,
				// we test regexps correctness.
				using (StreamWriter w = new StreamWriter(tmpLog, false, new UnicodeEncoding(false, false)))
					w.Write(GetSampleLog());
				ChangeEncodingToUnicode(createParams);

				using (RegularGrammar.UserDefinedFormatFactory f = new RegularGrammar.UserDefinedFormatFactory(createParams))
				{
					var cp = ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(tmpLog);
					using (var interaction = objectsFactory.CreateTestDialog())
					{
						testOk = interaction.ShowDialog(f, cp);
					}
				}

				UpdateView();
			}
			finally
			{
				File.Delete(tmpLog);
			}
		}

		private static void ChangeEncodingToUnicode(UserDefinedFactoryParams createParams)
		{
			var encodingNode = createParams.FormatSpecificNode.Element("encoding");
			if (encodingNode == null)
				createParams.FormatSpecificNode.Add(encodingNode = new XElement("encoding"));
			encodingNode.Value = "UTF-16";
		}

		void IViewEvents.OnChangeHeaderReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
			{
				interaction.ShowDialog(reGrammarRoot, true, this);
				UpdateView();
			}
		}

		void IViewEvents.OnChangeBodyReButtonClicked()
		{
			using (var interaction = objectsFactory.CreateEditRegexDialog())
			{
				interaction.ShowDialog(reGrammarRoot, false, this);
				UpdateView();
			}
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
			using (var interaction = new EditFieldsMappingInteraction(this, reGrammarRoot, allCaptures.ToArray()))
			{
				interaction.Start();
				UpdateView();
			}
		}

		string GetSampleLog()
		{
			if (sampleLogCache == null)
			{
				var sampleLogNode = reGrammarRoot.SelectSingleNode(sampleLogNodeName);
				if (sampleLogNode != null)
					sampleLogCache = sampleLogNode.InnerText;
				else
					sampleLogCache = "";
			}
			return sampleLogCache;
		}

		string ISampleLogAccess.SampleLog
		{
			get
			{
				return GetSampleLog();
			}
			set
			{
				sampleLogCache = value ?? "";
				var sampleLogNode = reGrammarRoot.SelectSingleNode(sampleLogNodeName);
				if (sampleLogNode == null)
					sampleLogNode = reGrammarRoot.AppendChild(reGrammarRoot.OwnerDocument.CreateElement(sampleLogNodeName));
				sampleLogNode.RemoveAll();
				sampleLogNode.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(sampleLogCache));
			}
		}

		class EditFieldsMappingInteraction : IFieldsMappingDialogViewEvents, IDisposable
		{
			readonly Presenter owner;
			static readonly string[] predefindOutputFields = { "Time", "Thread", "Body", "Severity" };
			int fieldIndex = 0;
			readonly XmlNode grammarRoot;
			bool updateLock;
			readonly string[] availableInputFields;
			IFieldsMappingDialogView dialog;
			List<Field> fieldsListBoxItems = new List<Field>();

			public EditFieldsMappingInteraction(Presenter owner, XmlNode root, string[] availableInputFields)
			{
				this.owner = owner;
				this.grammarRoot = root;
				this.availableInputFields = availableInputFields;

				this.dialog = owner.view.CreateFieldsMappingDialogView(this);

				InitAvailableFieldsList(availableInputFields);

				ReadMapping();
				UpdateView();
			}

			public void Dispose()
			{
				dialog.Dispose();
			}

			public void Start()
			{
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
				var newCode = dialog.ReadControl(FieldsMappingDialogControlId.CodeTextBox).Insert(selIdx, txt);
				dialog.ModifyControl(FieldsMappingDialogControlId.CodeTextBox, text: newCode);
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
					e.AppendChild(grammarRoot.OwnerDocument.CreateCDataSection(f.Code));
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
				dialog.ModifyControl(FieldsMappingDialogControlId.RemoveFieldButton, enabled: f != null);
				dialog.ModifyControl(FieldsMappingDialogControlId.NameComboBox, enabled: f != null);
				dialog.ModifyControl(FieldsMappingDialogControlId.CodeTypeComboBox, enabled: f != null);
				dialog.ModifyControl(FieldsMappingDialogControlId.CodeTextBox, enabled: f != null);
				dialog.ModifyControl(FieldsMappingDialogControlId.AvailableInputFieldsContainer, enabled: f != null);
				if (f != null)
				{
					dialog.ModifyControl(FieldsMappingDialogControlId.NameComboBox, text: f.Name);
					dialog.CodeTypeComboBoxSelectedIndex = (int)f.CodeType;
					dialog.ModifyControl(FieldsMappingDialogControlId.CodeTextBox, text: f.Code);
					dialog.SetControlOptions(FieldsMappingDialogControlId.NameComboBox,
						predefindOutputFields.Where(s => Get(s) == null).ToArray());
				}
				else
				{
					dialog.ModifyControl(FieldsMappingDialogControlId.NameComboBox, text: "");
					dialog.CodeTypeComboBoxSelectedIndex = -1;
					dialog.ModifyControl(FieldsMappingDialogControlId.CodeTextBox, text: "");
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

						owner.alerts.ShowPopup("Error", exception.ErrorMessage, AlertFlags.Ok | AlertFlags.WarningIcon);
						return;
					}
				}

				if (owner.alerts.ShowPopup(
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
					if ((fileName = owner.fileDialogs.SaveFileDialog(new SaveFileDialogParams()
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
							owner.alerts.ShowPopup("Error", "Failed to save file. " + e.Message, 
								AlertFlags.WarningIcon | AlertFlags.Ok);
						}
					}
				}
			}

			void IFieldsMappingDialogViewEvents.OnAddFieldButtonClicked()
			{
				Field f = new Field(string.Format("Field {0}", ++fieldIndex));
				fieldsListBoxItems.Add(f);
				dialog.AddFieldsListBoxItem(f.ToString());
				dialog.FieldsListBoxSelection = fieldsListBoxItems.Count - 1;
			}

			void IFieldsMappingDialogViewEvents.OnSelectedFieldChanged()
			{
				UpdateView();
			}

			void IFieldsMappingDialogViewEvents.OnRemoveFieldButtonClicked()
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

			void IFieldsMappingDialogViewEvents.OnNameComboBoxTextChanged()
			{
				Field f = Get();
				if (f == null)
					return;
				if (f.Name == dialog.ReadControl(FieldsMappingDialogControlId.NameComboBox))
					return;
				f.Name = dialog.ReadControl(FieldsMappingDialogControlId.NameComboBox);
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

			void IFieldsMappingDialogViewEvents.OnCodeTypeSelectedIndexChanged()
			{
				if (Get() != null)
					Get().CodeType = (FieldCodeType)dialog.CodeTypeComboBoxSelectedIndex;
			}

			void IFieldsMappingDialogViewEvents.OnCodeTextBoxChanged()
			{
				if (Get() != null)
					Get().Code = dialog.ReadControl(FieldsMappingDialogControlId.CodeTextBox);
			}

			void IFieldsMappingDialogViewEvents.OnOkClicked()
			{
				string msg = ValidateInput();
				if (msg != null)
				{
					owner.alerts.ShowPopup("Validation", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
					return;
				}
				WriteMapping();
				dialog.Close();
			}

			void IFieldsMappingDialogViewEvents.OnCancelClicked()
			{
				dialog.Close();
			}

			void IFieldsMappingDialogViewEvents.OnTestClicked(bool advancedModeModifierIsHeld)
			{
				XmlDocument tmp = new XmlDocument();
				XmlNode root = tmp.AppendChild(tmp.CreateElement("root"));
				XmlNode mapping = WriteMappingInternal(root);

				XDocument tmpXDoc = XDocument.Parse(tmp.OuterXml);

				FieldsProcessor.InitializationParams tmpProcessorParams = new FieldsProcessor.InitializationParams(
					tmpXDoc.Element("root").Element("fields-config"), false, null);
				try
				{
					FieldsProcessor tmpProcessor = new FieldsProcessor(tmpProcessorParams, availableInputFields, null, owner.tempFilesManager);
					tmpProcessor.Reset();
					owner.alerts.ShowPopup("Test", "Code compiled OK", AlertFlags.Ok);
				}
				catch (BadUserCodeException e)
				{
					HandleProcessorError(e, advancedModeModifierIsHeld);
				}
			}

			void IFieldsMappingDialogViewEvents.OnHelpLinkClicked()
			{
				owner.help.ShowHelp("FieldsMapping.htm");
			}
		};
	};
};