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

namespace LogJoint.UI
{
	public partial class EditRegexForm : Form
	{
		static HTMLColorsGenerator colors = new HTMLColorsGenerator();
		const int sampleLogTextLength = 1024 * 4;
		bool updateSampleEditLock = false;
		readonly bool headerReMode;
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
			return n != null ? n.InnerText : "";
		}


		public EditRegexForm(XmlNode reGrammarRoot, bool headerReMode, IProvideSampleLog provideSampleLog)
		{
			this.reGrammarRoot = reGrammarRoot;
			this.headerReMode = headerReMode;
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

			if (regExTextBox.Text.Length > 0)
				ExecRegex();
			else
				ResetReHilight();
		}

		[DllImport("user32.dll", EntryPoint="SendMessage", CharSet = CharSet.Auto)]
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
			}
			else
			{
				this.Text = "Edit body regular expression";
			}
		}

		void UpdateMatchesLabel(int matchesCount)
		{
			matchesCountLabel.Text = string.Format("{0}", matchesCount);
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr GetOleInterfaceMessage(HandleRef hWnd, int msg, int wParam, 
			[MarshalAs(UnmanagedType.IDispatch)] out object intf);

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
					foreach (MessageLocation loc in SplitToMessages())
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

		IEnumerable<MessageLocation> SplitToMessages()
		{
			string sample = sampleLogTextBox.Text;
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

		int ExecHeaderRe(Regex re)
		{
			string sample = sampleLogTextBox.Text;
			int matchCount = 0;

			for (int pos = 0; ; )
			{
				Match m = re.Match(sample, pos);
				if (!m.Success || m.Length == 0)
					break;

				colors.Reset();

				tomDoc.Range(m.Index, m.Index + m.Length).Font.Bold = -1;

				for (int i = 1; i < m.Groups.Count; ++i)
				{
					Group g = m.Groups[i];
					Color cl = colors.GenerateNewColor();
					tomDoc.Range(g.Index, g.Index + g.Length).Font.BackColor = ColorTranslator.ToWin32(cl);
					if (matchCount == 0)
					{
						capturesListBox.Items.Add(new ReCapture(cl, re.GroupNameFromNumber(i)));
					}
				}

				pos = m.Index + m.Length;
				++matchCount;
			}

			return matchCount;
		}

		int ExecBodyRe(Regex re)
		{
			string sample = sampleLogTextBox.Text;
			int matchCount = 0;

			foreach (MessageLocation loc in SplitToMessages())
			{
				Match m = re.Match(sample, loc.Begin + loc.HeaderLength, loc.TotalLength - loc.HeaderLength);
				if (!m.Success || m.Length == 0)
					continue;

				colors.Reset();

				tomDoc.Range(m.Index, m.Index + m.Length).Font.Bold = -1;

				for (int i = 1; i < m.Groups.Count; ++i)
				{
					Group g = m.Groups[i];
					Color cl = colors.GenerateNewColor();
					tomDoc.Range(g.Index, g.Index + g.Length).Font.BackColor = ColorTranslator.ToWin32(cl);
					if (matchCount == 0)
					{
						capturesListBox.Items.Add(new ReCapture(cl, re.GroupNameFromNumber(i)));
					}
				}

				++matchCount;
			}

			return matchCount;
		}

		void ExecRegex()
		{
			Regex re;
			try
			{
				re = new Regex(regExTextBox.Text, headerReMode ? headerReOptions : bodyReOptions);
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
				UpdateMatchesLabel(headerReMode ? ExecHeaderRe(re) : ExecBodyRe(re));
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
			n.RemoveAll();
			n.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(regExTextBox.Text));
			
			provideSampleLog.SampleLog = sampleLogTextBox.Text;
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

	}
}