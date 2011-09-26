using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace LogJoint.UI
{
	public partial class FormatDeleteConfirmPage : UserControl
	{
		public FormatDeleteConfirmPage()
		{
			InitializeComponent();

		}

		public void UpdateView(UserDefinedFormatsManager.UserDefinedFactoryBase factory)
		{
			messageLabel.Text = string.Format("You are about to delete '{0}' format definition. Press Finish to delete, Cancel to cancel the operation.",
				LogProviderFactoryRegistry.ToString(factory));

			descriptionTextBox.Text = factory.FormatDescription;
			fileNameTextBox.Text = factory.Location;

			if (File.Exists(factory.Location))
			{
				dateTextBox.Text = File.GetLastWriteTime(factory.Location).ToString();
			}
		}

	}
}
