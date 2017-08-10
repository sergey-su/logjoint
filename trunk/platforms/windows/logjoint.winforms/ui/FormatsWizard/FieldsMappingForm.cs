using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class FieldsMappingForm : Form, IFieldsMappingDialogView
	{
		readonly IFieldsMappingDialogViewEvents eventsHandler;

		public FieldsMappingForm(IFieldsMappingDialogViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			InitializeComponent();
			InitTabStops();
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendTabStopsMessage(HandleRef hWnd, int msg,
			int wParam, [In, MarshalAs(UnmanagedType.LPArray)] uint[] stops);

		void InitTabStops()
		{
			int EM_SETTABSTOPS = 0x00CB;
			SendTabStopsMessage(new HandleRef(codeTextBox, codeTextBox.Handle), EM_SETTABSTOPS, 1, new uint[] { 16 });
		}


		private void addFieldButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnAddFieldButtonClicked();
		}

		private void fieldsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSelectedFieldChanged();
		}

		private void removeFieldButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnRemoveFieldButtonClicked();
		}

		private void nameComboBox_TextUpdate(object sender, EventArgs e)
		{
			eventsHandler.OnNameComboBoxTextChanged();
		}

		private void codeTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnCodeTypeSelectedIndexChanged();
		}

		private void codeTextBox_TextChanged(object sender, EventArgs e)
		{
			eventsHandler.OnCodeTextBoxChanged();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnOkClicked();
		}
		
		private void testButton_Click(object sender, EventArgs evt)
		{
			eventsHandler.OnTestClicked((Control.ModifierKeys & Keys.Control) != 0);
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnHelpLinkClicked();
		}

		Control GetControl(FieldsMappingDialogControlId ctrl)
		{
			switch (ctrl)
			{
				case FieldsMappingDialogControlId.RemoveFieldButton: return removeFieldButton;
				case FieldsMappingDialogControlId.NameComboBox: return nameComboBox;
				case FieldsMappingDialogControlId.CodeTypeComboBox: return codeTypeComboBox;
				case FieldsMappingDialogControlId.CodeTextBox: return codeTextBox;
				case FieldsMappingDialogControlId.AvailableInputFieldsContainer: return availableInputFieldsPanel;
				default: return null;
			}
		}

		void IFieldsMappingDialogView.Show()
		{
			ShowDialog();
		}

		void IFieldsMappingDialogView.Close()
		{
			base.Close();
		}

		void IFieldsMappingDialogView.AddFieldsListBoxItem(string text)
		{
			fieldsListBox.Items.Add(text);
		}

		void IFieldsMappingDialogView.RemoveFieldsListBoxItem(int idx)
		{
			fieldsListBox.Items.RemoveAt(idx);
		}

		void IFieldsMappingDialogView.ChangeFieldsListBoxItem(int idx, string value)
		{
			fieldsListBox.Items[idx] = value;
		}

		void IFieldsMappingDialogView.ModifyControl(FieldsMappingDialogControlId id, string text, bool? enabled)
		{
			var ctrl = GetControl(id);
			if (text != null)
				ctrl.Text = text;
			if (enabled != null)
				ctrl.Enabled = enabled.Value;
		}

		void IFieldsMappingDialogView.SetControlOptions(FieldsMappingDialogControlId id, string[] options)
		{
			var cb = GetControl(id) as ComboBox;
			if (cb == null)
				return;
			cb.Items.Clear();
			cb.Items.AddRange(options);
		}

		void IFieldsMappingDialogView.SetAvailableInputFieldsLinks(Tuple<string, Action>[] links)
		{
			availableInputFieldsPanel.Controls.Clear();
			foreach (var f in links)
			{
				LinkLabel l = new LinkLabel();
				l.Text = f.Item1;
				l.AutoSize = true;
				l.Click += (s, e) => f.Item2();
				availableInputFieldsPanel.Controls.Add(l);
			}
		}

		string IFieldsMappingDialogView.ReadControl(FieldsMappingDialogControlId id)
		{
			return GetControl(id)?.Text;
		}

		void IFieldsMappingDialogView.ModifyCodeTextBoxSelection(int start, int len)
		{
			codeTextBox.SelectionStart = start;
			codeTextBox.SelectionLength = len;
			codeTextBox.Focus();
		}

		int IFieldsMappingDialogView.FieldsListBoxSelection
		{
			get { return fieldsListBox.SelectedIndex; }
			set { fieldsListBox.SelectedIndex = value; }
		}

		int IFieldsMappingDialogView.CodeTypeComboBoxSelectedIndex
		{
			get { return codeTypeComboBox.SelectedIndex; }
			set { codeTypeComboBox.SelectedIndex = value; }
		}

		int IFieldsMappingDialogView.CodeTextBoxSelectionStart => codeTextBox.SelectionStart;
	}
}