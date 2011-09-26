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
	public partial class FormatIdentityPage : UserControl, IWizardPage
	{
		XmlNode formatRoot;
		bool newFormatMode;

		public FormatIdentityPage(bool newFormatMode)
		{
			InitializeComponent();
			this.newFormatMode = newFormatMode;
			headerLabel.Text = newFormatMode ? "New format properties:" : "Format properties";
		}

		public void SetFormatRoot(XmlNode formatRoot)
		{
			this.formatRoot = formatRoot;
			XmlNode n;
			n = formatRoot.SelectSingleNode("id/@company");
			if (n != null)
				companyNameTextBox.Text = n.Value;

			n = formatRoot.SelectSingleNode("id/@name");
			if (n != null)
				formatNameTextBox.Text = n.Value;

			n = formatRoot.SelectSingleNode("description");
			if (n != null)
				descriptionTextBox.Text = n.InnerText;
		}

		public new string CompanyName { get { return this.companyNameTextBox.Text; } }
		public string FormatName { get { return this.formatNameTextBox.Text; } }
		public string Description { get { return this.descriptionTextBox.Text; } }

		bool ValidateInput()
		{
			string msg = null;
			if (FormatName == "")
			{
				msg = "Format name is mandatory";
				formatNameTextBox.Focus();
			}
			if (newFormatMode && LogProviderFactoryRegistry.DefaultInstance.Find(CompanyName, FormatName) != null)
			{
				msg = "Format with this company name/format name combination already exists";
				formatNameTextBox.Focus();
			}
			if (msg != null)
			{
				MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public bool ExitPage(bool movingForward)
		{
			if (movingForward && !ValidateInput())
				return false;

			XmlElement idNode = formatRoot.SelectSingleNode("id") as XmlElement;
			if (idNode == null)
				idNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("id")) as XmlElement;
			idNode.SetAttribute("company", CompanyName);
			idNode.SetAttribute("name", FormatName);

			XmlNode descNode = formatRoot.SelectSingleNode("description");
			if (descNode == null)
				descNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("description"));
			descNode.InnerText = Description;

			return true;
		}
	}

}
