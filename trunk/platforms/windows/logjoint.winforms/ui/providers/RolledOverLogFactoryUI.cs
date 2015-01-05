using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	public partial class RolledOverLogFactoryUI : UserControl, ILogProviderFactoryUI
	{
		public RolledOverLogFactoryUI()
		{
			InitializeComponent();
		}

		object ILogProviderFactoryUI.UIControl
		{
			get { return this; }
		}

		void ILogProviderFactoryUI.Apply(IModel model)
		{
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				pathTextBox.Text = folderBrowserDialog1.SelectedPath;
		}
	}
}
