using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Linq;

namespace LogJoint.UI
{
	public partial class EditRegexForm : Form
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
		readonly tom.ITextDocument tomDoc;
		readonly IProvideSampleLog provideSampleLog;

		static EditRegexForm()
		{
			RegexOptions baseOpts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;
			headerReOptions = baseOpts | RegexOptions.Multiline;
			bodyReOptions = baseOpts | RegexOptions.Singleline;
		}

		string ReadRe(XmlNode reGrammarRoot, string reNodeName)
		{
			XmlNode n = reGrammarRoot.SelectSingleNode(reNodeName);
			return n != null ? StringUtils.NormalizeLinebreakes(n.InnerText) : "";
		}


		public EditRegexForm(XmlNode reGrammarRoot, bool headerReMode, IProvideSampleLog provideSampleLog)
		{
			this.reGrammarRoot = reGrammarRoot;
			this.headerReMode = headerReMode;
			this.emptyReModeIsAllowed = !headerReMode;
			this.headerRe = ReadRe(reGrammarRoot, "head-re");
			this.bodyRe = ReadRe(reGrammarRoot, "body-re");
			this.provideSampleLog = provideSampleLog;

			InitializeComponent();

			using (Graphics g = this.CreateGraphics())
				capturesListBox.ItemHeight = (int)(14.0 * g.DpiY / 96.0);

			this.tomDoc = GetTextDocument();

			UpdateStaticTexts(headerReMode);

			regExTextBox.Text = headerReMode ? headerRe : bodyRe;
			regExTextBox.Select(0, 0);

			sampleLogTextBox.Text = provideSampleLog.SampleLog;

			UpdateMatchesLabel(0);
			InitTabStops();

			if (emptyReModeIsAllowed || string.IsNullOrWhiteSpace(regExTextBox.Text))
				ExecRegex();
			else
				ResetReHilight();

			UpdateEmptyReLabelVisibility();
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendTabStopsMessage(HandleRef hWnd, int msg,
			int wParam, [In, MarshalAs(UnmanagedType.LPArray)] uint[] stops);

		void InitTabStops()
		{
			int EM_SETTABSTOPS = 0x00CB;
			SendTabStopsMessage(new HandleRef(regExTextBox, regExTextBox.Handle), EM_SETTABSTOPS, 1,
				new uint[] { 16 });
		}

		class ReCapture
		{
			public readonly Color BkColor;
			public readonly string Name;
			public ReCapture(Color cl, string name)
			{
				BkColor = cl;
				Name = name;
			}
			public override string ToString()
			{
				return Name;
			}
		};

		void UpdateStaticTexts(bool headerReMode)
		{
			if (headerReMode)
			{
				this.Text = "Edit header regular expression";
				reHelpLabel.Text = @"This is a header regexp. Dot (.) matches every character including \n.  Do not use ^ and $ here.";
			}
			else
			{
				this.Text = "Edit body regular expression";
				reHelpLabel.Text = @"This is a body regexp. Dot (.) matches every character except \n. Use ^ and $ to match the boundaries of message body.";
			}
			if (emptyReModeIsAllowed)
			{
				emptyReLabel.Text = string.Format(
					"Leave body regular expression empty to match{1}the whole text between headers.{1}That is equivalent to {0} but is more efficient.",
					RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate, Environment.NewLine);
			}
		}

		void UpdateMatchesLabel(int matchesCount)
		{
			matchesCountLabel.Text = string.Format("{0}", matchesCount);
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr GetOleInterfaceMessage(HandleRef hWnd, int msg, int wParam, 
			[MarshalAs(UnmanagedType.IDispatch)] out object intf);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

		tom.ITextDocument GetTextDocument()
		{
			object intf;
			int EM_GETOLEINTERFACE = 0x43C;
			GetOleInterfaceMessage(new HandleRef(sampleLogTextBox, sampleLogTextBox.Handle), 
				EM_GETOLEINTERFACE, 0, out intf);
			return intf as tom.ITextDocument;
		}
		

		void ResetReHilight()
		{
			updateSampleEditLock = true;
			try
			{
				tom.ITextFont f1 = tomDoc.Range(0, 0xFFFFFF).Font;
				f1.BackColor = ColorTranslator.ToWin32(Color.White);
				f1.ForeColor = ColorTranslator.ToWin32(Color.Black);
				f1.Bold = 0;

				if (!headerReMode)
				{
					string sample = sampleLogTextBox.Text;
					foreach (MessageLocation loc in SplitToMessages(sample, headerRe))
					{
						tom.ITextFont f2 = tomDoc.Range(loc.Begin, loc.Begin + loc.HeaderLength).Font;
						f2.BackColor = ColorTranslator.ToWin32(Color.Gainsboro);
						f2.ForeColor = ColorTranslator.ToWin32(Color.DimGray);
					}
				}

				capturesListBox.Items.Clear();
				matchesCountLabel.Text = "0";

				execRegexButton.Enabled = sampleLogTextBox.Lines.Length > 0;
			}
			finally
			{
				updateSampleEditLock = false;
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
			for (; ; )
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
				Color cl = colors.GetNextColor(true).Color.ToColor();
				capturesListBox.Items.Add(new ReCapture(cl, re.GroupNameFromNumber(i)));
			}
		}

		private void ColorizeMatch(Match m)
		{
			colors.Reset();
			tomDoc.Range(m.Index, m.Index + m.Length).Font.Bold = -1;
			for (int i = 1; i < m.Groups.Count; ++i)
			{
				Group g = m.Groups[i];
				Color cl = colors.GetNextColor(true).Color.ToColor();
				tomDoc.Range(g.Index, g.Index + g.Length).Font.BackColor = ColorTranslator.ToWin32(cl);
			}
		}

		static int EvaluateRegexPerformance(IEnumerable<Match> testRegexRunner)
		{
			int millisecsToRunBenchmark = 50;

			int matchCount = 0;
			for (int benchmarkStarted = Environment.TickCount; (Environment.TickCount - benchmarkStarted) < millisecsToRunBenchmark; )
			{
				foreach (var m in testRegexRunner)
					++matchCount;
			}
			return matchCount/1000;
		}

		void EvaluatePerformanceAndUpdateControls(IEnumerable<Match> testRegexRunner)
		{
			int rating = EvaluateRegexPerformance(testRegexRunner);
			perfValueLabel.Text = rating.ToString();
		}

		static IEnumerable<Match> ExecHeaderRe(string sample, Regex re)
		{
			for (int pos = 0; ; )
			{
				Match m = re.Match(sample, pos);
				if (!m.Success || m.Length == 0)
					break;
				yield return m;
				pos = m.Index + m.Length;
			}
		}

		void ExecHeaderReAndUpdateControls(Regex re)
		{
			string sample = sampleLogTextBox.Text;
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
			string sample = sampleLogTextBox.Text;
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

		void ExecRegex()
		{
			Regex re;
			try
			{
				string reTxt;
				if (emptyReModeIsAllowed && string.IsNullOrWhiteSpace(regExTextBox.Text))
					reTxt = LogJoint.RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate;
				else
					reTxt = regExTextBox.Text;
				re = new Regex(reTxt, headerReMode ? headerReOptions : bodyReOptions);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Failed to parse regular expression", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				sampleLogTextBox.Select(0, 0);
			}
			finally
			{
				updateSampleEditLock = false;
			}
		}

		private void execRegexButton_Click(object sender, EventArgs e)
		{
			ExecRegex();
		}

		private void regExTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5)
				ExecRegex();
		}

		private void capturesListBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0)
				return;
			ReCapture c = capturesListBox.Items[e.Index] as ReCapture;
			if (c == null)
				return;
			using (SolidBrush b = new SolidBrush(c.BkColor))
				e.Graphics.FillRectangle(b, e.Bounds);
			e.Graphics.DrawString(c.Name, this.Font, Brushes.Black, e.Bounds);
		}

		private void sampleLogTextBox_TextChanged(object sender, EventArgs e)
		{
			if (!updateSampleEditLock)
				ResetReHilight();
		}

		string ValidateInput()
		{
			return null;
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
			n.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(regExTextBox.Text));
			
			// Reading sample log text back to IProvideSampleLog object 
			// SaveFile() is used here instead of Text propertly because 
			// Text getter produces \n instead of \r\n. I need \r\n because
			// the sample log might be loaded into simple TextBox on EditSampleLogForm.
			// TextBox wants only \r\n.
			MemoryStream stm = new MemoryStream();
			sampleLogTextBox.SaveFile(stm, RichTextBoxStreamType.UnicodePlainText);
			string unicodeText = Encoding.Unicode.GetString(stm.GetBuffer(), 0, (int)stm.Length);
			provideSampleLog.SampleLog = unicodeText;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			string validationError = ValidateInput();

			if (validationError != null)
			{
				MessageBox.Show(validationError, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			SaveData();

			DialogResult = DialogResult.OK;
		}

		private void conceptsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Help.ShowHelp("HowRegexParsingWorks.htm");
		}

		private void regexSyntaxLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Help.ShowHelp("http://msdn.microsoft.com/en-us/library/1400241x(VS.85).aspx");
		}

		private void panel1_Layout(object sender, LayoutEventArgs e)
		{
			emptyReLabel.Location = new Point(
				(panel1.Size.Width - emptyReLabel.Size.Width) / 2,
				(panel1.Size.Height - SystemInformation.HorizontalScrollBarHeight - emptyReLabel.Size.Height) / 2
			);
		}

		void UpdateEmptyReLabelVisibility()
		{
			emptyReLabel.Visible = emptyReModeIsAllowed && string.IsNullOrWhiteSpace(regExTextBox.Text);
		}

		private void regExTextBox_TextChanged(object sender, EventArgs e)
		{
			UpdateEmptyReLabelVisibility();
		}
	}
}