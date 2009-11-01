using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace LogJoint.UI
{
	public partial class SaveFormatPage : UserControl, IWizardPage
	{
		XmlDocument doc;
		bool newFormatMode;

		public SaveFormatPage(bool newFormatMode)
		{
			this.newFormatMode = newFormatMode;
			InitializeComponent();
			UpdateView();
		}

		public void SetDocument(XmlDocument doc)
		{
			this.doc = doc;
		}


		bool ValidateInput()
		{
			string basis = GetValidBasis();
			if (basis == null)
			{
				MessageBox.Show("File name basis is invalid.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			string fname = UserDefinedFormatsManager.Instance.GetFullFormatFileName(basis);
			if (newFormatMode && System.IO.File.Exists(fname))
			{
				if (MessageBox.Show("File alredy exists. Overwrite?", "Validation",
					MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes)
				{
					return false;
				}
			}
			return true;
		}

		public string FileNameBasis
		{
			get { return fileNameBasisTextBox.Text; }
			set { fileNameBasisTextBox.Text = value; UpdateView(); }
		}

		public bool ExitPage(bool movingForward)
		{
			if (!movingForward)
				return true;

			if (!ValidateInput())
				return false;

			try
			{
				doc.Save(FileName);
			}
			catch (Log4NetImportException e)
			{
				MessageBox.Show("Failed to save the format:\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public string FileName
		{
			get { return fileNameTextBox.Text; }
		}

		string GetValidBasis()
		{
			string ret = fileNameBasisTextBox.Text;
			ret = ret.Trim();
			if (ret == "")
				return null;
			if (ret.IndexOfAny(new char[] { '\\', '/', ':', '"', '<', '>', '|', '?', '*' }) >= 0)
				return null;
			return ret;
		}

		void UpdateView()
		{
			string basis = GetValidBasis();
			if (basis == null)
				fileNameTextBox.Text = "";
			else
				fileNameTextBox.Text = UserDefinedFormatsManager.Instance.GetFullFormatFileName(basis);
		}

		private void fileNameBasisTextBox_TextChanged(object sender, EventArgs e)
		{
			UpdateView();
		}
	}
}
