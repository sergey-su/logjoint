using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ChooseExistingFormatPage : UserControl
	{
		private IWizardScenarioHost host;

		public ChooseExistingFormatPage(IWizardScenarioHost host)
		{
			this.host = host;

			InitializeComponent();

			LoadFormatsList();
		}

		void LoadFormatsList()
		{
			foreach (UserDefinedFactoryBase f in host.Model.UserDefinedFormatsManager.Items)
			{
				formatsListBox.Items.Add(f);
			}
		}

		public UserDefinedFactoryBase GetSelectedFormat()
		{
			if (formatsListBox.SelectedIndex < 0)
				return null;
			return formatsListBox.Items[formatsListBox.SelectedIndex] as UserDefinedFactoryBase;
		}

		string ValidateInputInternal()
		{
			if (GetSelectedFormat() == null)
			{
				return "Select a format";
			}
			if (!changeFmtRadioButton.Checked
			 && !deleteFmtRadioButton.Checked)
			{
				return "Select action to perform";
			}
			return null;
		}

		public bool ValidateInput()
		{
			string msg = ValidateInputInternal();
			if (msg == null)
				return true;
			MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}

		void TryToGoNext()
		{
			if (ValidateInputInternal() != null)
				return;
			host.Next();
		}

		private void formatsListBox_DoubleClick(object sender, EventArgs e)
		{
			TryToGoNext();
		}

		private void deleteFmtRadioButton_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && e.Clicks >= 2)
				TryToGoNext();
		}
	}
}
