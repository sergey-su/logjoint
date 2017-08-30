using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters.FormatsWizard.EditRegexDialog;

namespace LogJoint.UI
{
	public partial class EditRegexForm : Form, IView
	{
		IViewEvents eventsHandler;
		readonly tom.ITextDocument tomDoc;

		public EditRegexForm()
		{
			InitializeComponent();

			using (Graphics g = this.CreateGraphics())
				capturesListBox.ItemHeight = (int)(14.0 * g.DpiY / 96.0);

			this.tomDoc = GetTextDocument();

			InitTabStops();
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

		private void execRegexButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnExecRegexButtonClicked();
		}

		private void regExTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5)
				eventsHandler.OnExecRegexShortcut();
		}

		private void capturesListBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0)
				return;
			CapturesListBoxItem c = capturesListBox.Items[e.Index] as CapturesListBoxItem;
			if (c == null)
				return;
			using (SolidBrush b = new SolidBrush(c.Color.ToColor()))
				e.Graphics.FillRectangle(b, e.Bounds);
			e.Graphics.DrawString(c.Text, this.Font, Brushes.Black, e.Bounds);
		}

		private void sampleLogTextBox_TextChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSampleEditTextChanged();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCloseButtonClicked(accepted: true);
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCloseButtonClicked(accepted: false);
		}

		private void conceptsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnConceptsLinkClicked();
		}

		private void regexSyntaxLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnRegexHelpLinkClicked();
		}

		private void panel1_Layout(object sender, LayoutEventArgs e)
		{
			emptyReLabel.Location = new Point(
				(panel1.Size.Width - emptyReLabel.Size.Width) / 2,
				(panel1.Size.Height - SystemInformation.HorizontalScrollBarHeight - emptyReLabel.Size.Height) / 2
			);
		}

		private void regExTextBox_TextChanged(object sender, EventArgs e)
		{
			eventsHandler.OnRegExTextBoxTextChanged();
		}

		Control GetCtrl(ControlId ctrl)
		{
			switch (ctrl)
			{
				case ControlId.Dialog: return this;
				case ControlId.RegExTextBox: return regExTextBox;
				case ControlId.SampleLogTextBox: return sampleLogTextBox;
				case ControlId.ReHelpLabel: return reHelpLabel;
				case ControlId.EmptyReLabel: return emptyReLabel;
				case ControlId.MatchesCountLabel: return matchesCountLabel;
				case ControlId.PerfValueLabel: return perfValueLabel;
				case ControlId.LegendLabel: return label3;
				case ControlId.LegendList: return capturesListBox;
				default: return null;
			}
		}

		void IView.Show()
		{
			ShowDialog();
		}

		void IView.Close()
		{
			base.Close();
		}

		string IView.ReadControl(ControlId ctrl)
		{
			return GetCtrl(ctrl)?.Text;
		}

		void IView.WriteControl(ControlId ctrl, string value)
		{
			var obj = GetCtrl(ctrl);
			if (obj != null)
				obj.Text = value;
		}

		void IView.ClearCapturesListBox()
		{
			capturesListBox.Items.Clear();
		}

		void IView.EnableControl(ControlId ctrl, bool enable)
		{
			var obj = GetCtrl(ctrl);
			if (obj != null)
				obj.Enabled = enable;
		}

		void IView.SetControlVisibility(ControlId ctrl, bool value)
		{
			var obj = GetCtrl(ctrl);
			if (obj != null)
				obj.Visible = value;
		}

		void IView.AddCapturesListBoxItem(CapturesListBoxItem item)
		{
			capturesListBox.Items.Add(item);
		}

		void IView.ResetSelection(ControlId ctrl)
		{
			(GetCtrl(ctrl) as TextBox)?.Select(0, 0);
		}

		void IView.PatchLogSample(TextPatch p)
		{
			tom.ITextFont fnt = tomDoc.Range(p.RangeBegin, p.RangeEnd).Font;
			Func<ModelColor?, int> translatorColor = cl => ColorTranslator.ToWin32(cl.Value.ToColor());
			if (p.BackColor != null)
				fnt.BackColor = translatorColor(p.BackColor);
			if (p.ForeColor != null)
				fnt.ForeColor = translatorColor(p.ForeColor);
			if (p.Bold != null)
				fnt.Bold = p.Bold.Value ? -1 : 0;
		}

		void IView.SetEventsHandler(IViewEvents events)
		{
			this.eventsHandler = events;
		}
	}
}