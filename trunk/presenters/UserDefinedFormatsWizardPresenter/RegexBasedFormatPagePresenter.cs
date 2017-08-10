using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage
{
	internal class Presenter : IPresenter, IViewEvents, IEditSampleDialogViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		XmlNode formatRoot;
		XmlNode reGrammarRoot;
		bool testOk;
		static readonly string[] parameterStatusStrings = new string[] { "Not set", "OK" };
		static readonly string[] testStatusStrings = new string[] { "", "Passed" };
		static readonly string sampleLogNodeName = "sample-log";
		string sampleLogCache = null;
		IEditSampleDialogView editSampleDialog;

		public Presenter(
			IView view, 
			IWizardScenarioHost host,
			Help.IPresenter help, 
			ITempFilesManager tempFilesManager,
			IAlertPopup alerts,
			IFileDialogs fileDialogs
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.help = help;
			this.tempFilesManager = tempFilesManager;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
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
			InitStatusLabel(ControlId.SampleLogStatusLabel, SampleLog != "", parameterStatusStrings);
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
			using (editSampleDialog = view.CreateEditSampleDialog(this))
			{
				editSampleDialog.SampleLogTextBoxValue = SampleLog;
				editSampleDialog.Show();
			}
			editSampleDialog = null;
			UpdateView();
		}

		void IViewEvents.OnTestButtonClicked()
		{
			if (SampleLog == "")
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
					w.Write(SampleLog);
				ChangeEncodingToUnicode(createParams);

				using (RegularGrammar.UserDefinedFormatFactory f = new RegularGrammar.UserDefinedFormatFactory(createParams))
				{
					var cp = ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(tmpLog);
					//todo
					//testOk = TestParserForm.Execute(f, cp, tempFilesManager, logViewerPresenterFactory);
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
			using (var interaction = new EditRegexInteraction(this, reGrammarRoot, true))
			{
				interaction.Start();
				UpdateView();
			}
		}

		void IViewEvents.OnChangeBodyReButtonClicked()
		{
			using (var interaction = new EditRegexInteraction(this, reGrammarRoot, false))
			{
				interaction.Start();
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

		void IEditSampleDialogViewEvents.OnCloseButtonClicked(bool accepted)
		{
			if (accepted)
				SampleLog = editSampleDialog.SampleLogTextBoxValue;
			editSampleDialog.Close();
		}

		void IEditSampleDialogViewEvents.OnLoadSampleButtonClicked()
		{
			var fileName = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
			{
				CanChooseFiles = true
			})?.FirstOrDefault();
			if (fileName == null)
				return;
			try
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader r = new StreamReader(fs, Encoding.ASCII, true))
				{
					char[] buf = new char[1024 * 4];
					var sample = new string(buf, 0, r.Read(buf, 0, buf.Length));
					editSampleDialog.SampleLogTextBoxValue = sample;
				}
			}
			catch (Exception ex)
			{
				alerts.ShowPopup("Error", "Failed to read the file: " + ex.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
			}
		}

		string SampleLog
		{
			get
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

		class EditRegexInteraction : IEditRegexDialogViewEvents, IDisposable
		{
			static HTMLColorsGenerator colors = new HTMLColorsGenerator();
			const int sampleLogTextLength = 1024 * 4;
			bool updateSampleEditLock = false;
			readonly bool headerReMode;
			readonly bool emptyReModeIsAllowed;
			readonly string headerRe;
			readonly string bodyRe;
			readonly XmlNode reGrammarRoot;
			readonly static RegexOptions headerReOptions;
			readonly static RegexOptions bodyReOptions;
			readonly Presenter owner;
			readonly IEditRegexDialogView dialog;

			static EditRegexInteraction()
			{
				RegexOptions baseOpts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;
				headerReOptions = baseOpts | RegexOptions.Multiline;
				bodyReOptions = baseOpts | RegexOptions.Singleline;
			}

			public EditRegexInteraction(Presenter owner, XmlNode reGrammarRoot, bool headerReMode)
			{
				this.owner = owner;
				this.dialog = owner.view.CreateEditRegexDialog(this);
				this.reGrammarRoot = reGrammarRoot;
				this.headerReMode = headerReMode;
				this.emptyReModeIsAllowed = !headerReMode;
				this.headerRe = ReadRe(reGrammarRoot, "head-re");
				this.bodyRe = ReadRe(reGrammarRoot, "body-re");

				UpdateStaticTexts(headerReMode);

				dialog.WriteControl(EditRegexDialogControlId.RegExTextBox, headerReMode ? headerRe : bodyRe);
				dialog.ResetSelection(EditRegexDialogControlId.RegExTextBox);

				dialog.WriteControl(EditRegexDialogControlId.SampleLogTextBox, owner.SampleLog);

				UpdateMatchesLabel(0);

				if (emptyReModeIsAllowed || string.IsNullOrWhiteSpace(dialog.ReadControl(EditRegexDialogControlId.RegExTextBox)))
					ExecRegex();
				else
					ResetReHilight();

				UpdateEmptyReLabelVisibility();
			}

			public void Dispose()
			{
				dialog.Dispose();
			}

			public void Start()
			{
				dialog.Show();
			}

			string ReadRe(XmlNode reGrammarRoot, string reNodeName)
			{
				XmlNode n = reGrammarRoot.SelectSingleNode(reNodeName);
				return n != null ? StringUtils.NormalizeLinebreakes(n.InnerText) : "";
			}

			void UpdateStaticTexts(bool headerReMode)
			{
				if (headerReMode)
				{
					dialog.WriteControl(EditRegexDialogControlId.Dialog, "Edit header regular expression");
					dialog.WriteControl(EditRegexDialogControlId.ReHelpLabel,
						@"This is a header regexp. Dot (.) matches every character including \n.  Do not use ^ and $ here.");
				}
				else
				{
					dialog.WriteControl(EditRegexDialogControlId.Dialog, "Edit body regular expression");
					dialog.WriteControl(EditRegexDialogControlId.ReHelpLabel,
						@"This is a body regexp. Dot (.) matches every character except \n. Use ^ and $ to match the boundaries of message body.");
				}
				if (emptyReModeIsAllowed)
				{
					dialog.WriteControl(EditRegexDialogControlId.EmptyReLabel, string.Format(
						"Leave body regular expression empty to match{1}the whole text between headers.{1}That is equivalent to {0} but is more efficient.",
						RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate, Environment.NewLine));
				}
			}

			void UpdateMatchesLabel(int matchesCount)
			{
				dialog.WriteControl(EditRegexDialogControlId.MatchesCountLabel, string.Format("{0}", matchesCount));
			}

			void ExecRegex()
			{
				Regex re;
				try
				{
					string reTxt;
					if (emptyReModeIsAllowed && string.IsNullOrWhiteSpace(dialog.ReadControl(EditRegexDialogControlId.RegExTextBox)))
						reTxt = RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate;
					else
						reTxt = dialog.ReadControl(EditRegexDialogControlId.RegExTextBox);
					re = new Regex(reTxt, headerReMode ? headerReOptions : bodyReOptions);
				}
				catch (Exception e)
				{
					owner.alerts.ShowPopup("Failed to parse regular expression", e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
					return;
				}

				ResetReHilight();

				updateSampleEditLock = true;
				try
				{
					if (headerReMode)
						ExecHeaderReAndUpdateControls(re);
					else
						ExecBodyReAndUpdateConstrol(re);
					dialog.ResetSelection(EditRegexDialogControlId.SampleLogTextBox);
				}
				finally
				{
					updateSampleEditLock = false;
				}
			}

			void ResetReHilight()
			{
				updateSampleEditLock = true;
				try
				{
					dialog.PatchLogSample(new TextPatch()
					{
						RangeBegin = 0,
						RangeEnd = 0xFFFFFF,
						BackColor = new ModelColor(0xffffffff),
						ForeColor = new ModelColor(0xff000000),
						Bold = false
					});

					if (!headerReMode)
					{
						string sample = dialog.ReadControl(EditRegexDialogControlId.SampleLogTextBox);
						foreach (MessageLocation loc in SplitToMessages(sample, headerRe))
						{
							dialog.PatchLogSample(new TextPatch()
							{
								RangeBegin = loc.Begin,
								RangeEnd = loc.Begin + loc.HeaderLength,
								BackColor = new ModelColor(0xFFDCDCDC),
								ForeColor = new ModelColor(0xFF696969)
							});
						}
					}

					dialog.ClearCapturesListBox();
					dialog.WriteControl(EditRegexDialogControlId.MatchesCountLabel, "0");

					dialog.EnableControl(EditRegexDialogControlId.RegExTextBox, dialog.ReadControl(EditRegexDialogControlId.SampleLogTextBox).Length > 0);
				}
				finally
				{
					updateSampleEditLock = false;
				}
			}

			void ExecHeaderReAndUpdateControls(Regex re)
			{
				string sample = dialog.ReadControl(EditRegexDialogControlId.SampleLogTextBox);
				int matchCount = 0;

				foreach (Match m in ExecHeaderRe(sample, re))
				{
					ColorizeMatch(m);
					if (matchCount == 0)
						FillCapturesListBox(m, re);
					++matchCount;
				}

				UpdateMatchesLabel(matchCount);

				EvaluatePerformanceAndUpdateControls(ExecHeaderRe(sample, re));
			}

			static IEnumerable<Match> ExecBodyRe(string sample, IEnumerable<MessageLocation> messagesLocations, Regex bodyRe)
			{
				foreach (var loc in messagesLocations)
				{
					Match m = bodyRe.Match(sample, loc.Begin + loc.HeaderLength, loc.TotalLength - loc.HeaderLength);
					if (!m.Success || m.Length == 0)
						continue;
					yield return m;
				}
			}

			void ExecBodyReAndUpdateConstrol(Regex bodyRe)
			{
				string sample = dialog.ReadControl(EditRegexDialogControlId.SampleLogTextBox);
				int matchCount = 0;

				var messages = SplitToMessages(sample, headerRe).ToList();

				foreach (Match m in ExecBodyRe(sample, messages, bodyRe))
				{
					ColorizeMatch(m);
					if (matchCount == 0)
						FillCapturesListBox(m, bodyRe);
					++matchCount;
				}

				UpdateMatchesLabel(matchCount);

				EvaluatePerformanceAndUpdateControls(ExecBodyRe(sample, messages, bodyRe));
			}

			static IEnumerable<Match> ExecHeaderRe(string sample, Regex re)
			{
				for (int pos = 0; ;)
				{
					Match m = re.Match(sample, pos);
					if (!m.Success || m.Length == 0)
						break;
					yield return m;
					pos = m.Index + m.Length;
				}
			}

			struct MessageLocation
			{
				public int Begin;
				public int TotalLength;
				public int HeaderLength;
			};

			static IEnumerable<MessageLocation> SplitToMessages(string sample, string headerRe)
			{
				Regex re = new Regex(headerRe, headerReOptions);
				int pos = 0;
				MessageLocation loc = new MessageLocation();
				for (;;)
				{
					Match m = re.Match(sample, pos);
					if (!m.Success || m.Length == 0)
						break;

					if (loc.HeaderLength != 0)
					{
						loc.TotalLength = m.Index - loc.Begin;
						yield return loc;
					}

					loc.Begin = m.Index;
					loc.HeaderLength = m.Length;

					pos = m.Index + m.Length;
				}

				if (loc.HeaderLength != 0)
				{
					loc.TotalLength = sample.Length - loc.Begin;
					yield return loc;
				}
			}

			private void FillCapturesListBox(Match m, Regex re)
			{
				colors.Reset();
				for (int i = 1; i < m.Groups.Count; ++i)
				{
					dialog.AddCapturesListBoxItem(new CapturesListBoxItem()
					{
						Text = re.GroupNameFromNumber(i),
						Color = colors.GetNextColor(true, null).Color
					});
				}
			}

			private void ColorizeMatch(Match m)
			{
				colors.Reset();
				dialog.PatchLogSample(new TextPatch()
				{
					RangeBegin = m.Index,
					RangeEnd = m.Index + m.Length,
					Bold = true
				});
				for (int i = 1; i < m.Groups.Count; ++i)
				{
					Group g = m.Groups[i];
					var cl = colors.GetNextColor(true, null).Color;
					dialog.PatchLogSample(new TextPatch()
					{
						RangeBegin = g.Index,
						RangeEnd = g.Index + g.Length,
						BackColor = cl
					});
				}
			}

			static int EvaluateRegexPerformance(IEnumerable<Match> testRegexRunner)
			{
				int millisecsToRunBenchmark = 50;

				int matchCount = 0;
				for (int benchmarkStarted = Environment.TickCount; (Environment.TickCount - benchmarkStarted) < millisecsToRunBenchmark;)
				{
					foreach (var m in testRegexRunner)
						++matchCount;
				}
				return matchCount / 1000;
			}

			void EvaluatePerformanceAndUpdateControls(IEnumerable<Match> testRegexRunner)
			{
				int rating = EvaluateRegexPerformance(testRegexRunner);
				dialog.WriteControl(EditRegexDialogControlId.PerfValueLabel, rating.ToString());
			}

			void SaveData()
			{
				string nodeName = headerReMode ? "head-re" : "body-re";
				XmlNode n = reGrammarRoot.SelectSingleNode(nodeName);
				if (n == null)
					n = reGrammarRoot.AppendChild(reGrammarRoot.OwnerDocument.CreateElement(nodeName));
				var texts = n.ChildNodes.Cast<XmlNode>().Where(c => c.NodeType == XmlNodeType.CDATA || c.NodeType == XmlNodeType.Text).ToArray();
				foreach (var t in texts) // remove all texts and CDATAs preserving attributes
					n.RemoveChild(t);
				n.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(dialog.ReadControl(EditRegexDialogControlId.RegExTextBox)));

				owner.SampleLog = dialog.ReadControl(EditRegexDialogControlId.SampleLogTextBox);
			}

			void UpdateEmptyReLabelVisibility()
			{
				dialog.SetControlVisibility(EditRegexDialogControlId.EmptyReLabel, 
					emptyReModeIsAllowed && string.IsNullOrWhiteSpace(dialog.ReadControl(EditRegexDialogControlId.RegExTextBox)));
			}

			void IEditRegexDialogViewEvents.OnExecRegexButtonClicked()
			{
				ExecRegex();
			}

			void IEditRegexDialogViewEvents.OnExecRegexShortcut()
			{
				ExecRegex();
			}

			void IEditRegexDialogViewEvents.OnSampleEditTextChanged()
			{
				if (!updateSampleEditLock)
					ResetReHilight();
			}

			void IEditRegexDialogViewEvents.OnCloseButtonClicked(bool accepted)
			{
				if (accepted)
					SaveData();
				dialog.Close();
			}

			void IEditRegexDialogViewEvents.OnConceptsLinkClicked()
			{
				owner.help.ShowHelp("HowRegexParsingWorks.htm");
			}

			void IEditRegexDialogViewEvents.OnRegexHelpLinkClicked()
			{
				owner.help.ShowHelp("http://msdn.microsoft.com/en-us/library/1400241x(VS.85).aspx");
			}

			void IEditRegexDialogViewEvents.OnRegExTextBoxTextChanged()
			{
				UpdateEmptyReLabelVisibility();
			}
		};


		class EditFieldsMappingInteraction : IFieldsMappingDialogViewEvents, IDisposable
		{
			readonly Presenter owner;
			static readonly string[] predefindOutputFields = new string[]
				{ "Time", "Thread", "Body", "Severity", "EntryType" };
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
				int selIdx = dialog.CodeTextBoxSelectionStart;
				dialog.ModifyControl(FieldsMappingDialogControlId.CodeTextBox, text:
					dialog.ReadControl(FieldsMappingDialogControlId.CodeTextBox).Insert(selIdx, txt));
				dialog.ModifyCodeTextBoxSelection(selIdx + txt.Length, 0);
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