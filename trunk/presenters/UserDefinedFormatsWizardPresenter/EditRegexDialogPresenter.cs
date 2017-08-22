using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard.EditRegexDialog
{
	internal class Presenter : IPresenter, IDisposable, IViewEvents
	{
		readonly IView dialog;
		readonly Help.IPresenter help;
		readonly IAlertPopup alerts;
		static HTMLColorsGenerator colors = new HTMLColorsGenerator();
		const int sampleLogTextLength = 1024 * 4;
		bool updateSampleEditLock = false;
		bool headerReMode;
		bool emptyReModeIsAllowed;
		string headerRe;
		string bodyRe;
		XmlNode reGrammarRoot;
		ISampleLogAccess sampleLog;
		readonly static RegexOptions headerReOptions;
		readonly static RegexOptions bodyReOptions;

		static Presenter()
		{
			RegexOptions baseOpts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;
			headerReOptions = baseOpts | RegexOptions.Multiline;
			bodyReOptions = baseOpts | RegexOptions.Singleline;
		}

		public Presenter(
			IView view, 
			Help.IPresenter help, 
			IAlertPopup alerts
		)
		{
			this.dialog = view;
			this.dialog.SetEventsHandler(this);
			this.help = help;
			this.alerts = alerts;
		}

		void IDisposable.Dispose ()
		{
			dialog.Dispose();
		}

		void IPresenter.ShowDialog(
			XmlNode reGrammarRoot, 
			bool headerReMode,
			ISampleLogAccess sampleLog
		)
		{
			this.reGrammarRoot = reGrammarRoot;
			this.headerReMode = headerReMode;
			this.sampleLog = sampleLog;
			this.emptyReModeIsAllowed = !headerReMode;
			this.headerRe = ReadRe(reGrammarRoot, "head-re");
			this.bodyRe = ReadRe(reGrammarRoot, "body-re");

			UpdateStaticTexts(headerReMode);

			dialog.WriteControl(ControlId.RegExTextBox, headerReMode ? headerRe : bodyRe);
			dialog.ResetSelection(ControlId.RegExTextBox);

			dialog.WriteControl(ControlId.SampleLogTextBox, sampleLog.SampleLog);

			UpdateMatchesLabel(0);

			if (emptyReModeIsAllowed || string.IsNullOrWhiteSpace(dialog.ReadControl(ControlId.RegExTextBox)))
				ExecRegex();
			else
				ResetReHilight();

			UpdateEmptyReLabelVisibility();

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
				dialog.WriteControl(ControlId.Dialog, "Edit header regular expression");
				dialog.WriteControl(ControlId.ReHelpLabel,
					@"This is a header regexp. Dot (.) matches every character including \n.  Do not use ^ and $ here.");
			}
			else
			{
				dialog.WriteControl(ControlId.Dialog, "Edit body regular expression");
				dialog.WriteControl(ControlId.ReHelpLabel,
					@"This is a body regexp. Dot (.) matches every character except \n. Use ^ and $ to match the boundaries of message body.");
			}
			if (emptyReModeIsAllowed)
			{
				dialog.WriteControl(ControlId.EmptyReLabel, string.Format(
					"Leave body regular expression empty to match{1}the whole text between headers.{1}That is equivalent to {0} but is more efficient.",
					RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate, Environment.NewLine));
			}
		}

		void UpdateMatchesLabel(int matchesCount)
		{
			dialog.WriteControl(ControlId.MatchesCountLabel, string.Format("{0}", matchesCount));
		}

		void ExecRegex()
		{
			Regex re;
			try
			{
				string reTxt;
				if (emptyReModeIsAllowed && string.IsNullOrWhiteSpace(dialog.ReadControl(ControlId.RegExTextBox)))
					reTxt = RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate;
				else
					reTxt = dialog.ReadControl(ControlId.RegExTextBox);
				re = new Regex(reTxt, headerReMode ? headerReOptions : bodyReOptions);
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Failed to parse regular expression", e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
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
				dialog.ResetSelection(ControlId.SampleLogTextBox);
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
				string sample = dialog.ReadControl(ControlId.SampleLogTextBox);

				dialog.PatchLogSample(new TextPatch()
				{
					RangeBegin = 0,
					RangeEnd = sample.Length,
					BackColor = new ModelColor(0xffffffff),
					ForeColor = new ModelColor(0xff000000),
					Bold = false
				});

				if (!headerReMode)
				{
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
				dialog.WriteControl(ControlId.MatchesCountLabel, "0");

				dialog.EnableControl(ControlId.RegExTextBox, dialog.ReadControl(ControlId.SampleLogTextBox).Length > 0);
			}
			finally
			{
				updateSampleEditLock = false;
			}
		}

		void ExecHeaderReAndUpdateControls(Regex re)
		{
			string sample = dialog.ReadControl(ControlId.SampleLogTextBox);
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
			string sample = dialog.ReadControl(ControlId.SampleLogTextBox);
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
			Regex re;
			try
			{
				re = new Regex(headerRe, headerReOptions);
			}
			catch 
			{
				yield break;
			}
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
			dialog.WriteControl(ControlId.PerfValueLabel, rating.ToString());
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
			n.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(dialog.ReadControl(ControlId.RegExTextBox)));

			sampleLog.SampleLog = dialog.ReadControl(ControlId.SampleLogTextBox);
		}

		void UpdateEmptyReLabelVisibility()
		{
			dialog.SetControlVisibility(
				ControlId.EmptyReLabel, 
				emptyReModeIsAllowed && string.IsNullOrWhiteSpace(dialog.ReadControl(ControlId.RegExTextBox))
			);
		}

		void IViewEvents.OnExecRegexButtonClicked()
		{
			ExecRegex();
		}

		void IViewEvents.OnExecRegexShortcut()
		{
			ExecRegex();
		}

		void IViewEvents.OnSampleEditTextChanged()
		{
			if (!updateSampleEditLock)
				ResetReHilight();
		}

		void IViewEvents.OnCloseButtonClicked(bool accepted)
		{
			if (accepted)
				SaveData();
			dialog.Close();
		}

		void IViewEvents.OnConceptsLinkClicked()
		{
			help.ShowHelp("HowRegexParsingWorks.htm");
		}

		void IViewEvents.OnRegexHelpLinkClicked()
		{
			help.ShowHelp("http://msdn.microsoft.com/en-us/library/1400241x(VS.85).aspx");
		}

		void IViewEvents.OnRegExTextBoxTextChanged()
		{
			UpdateEmptyReLabelVisibility();
		}
	};
};