using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage
{
	internal class Presenter : IPresenter, IViewEvents, ISampleLogAccess
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly Help.IPresenter help;
		readonly ITempFilesManager tempFilesManager;
		readonly IAlertPopup alerts;
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
			using (var interaction = objectsFactory.CreateEditFieldsMapping())
			{
				interaction.ShowDialog(reGrammarRoot, allCaptures.ToArray());
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
	};
};