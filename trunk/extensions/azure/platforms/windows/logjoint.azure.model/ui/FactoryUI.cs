using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace LogJoint.Azure
{
	public partial class FactoryUI : UserControl, ILogProviderFactoryUI
	{
		Azure.Factory factory;

		public FactoryUI(Azure.Factory factory)
		{
			this.factory = factory;
			InitializeComponent();
			UpdateControls();
		}
		
		#region ILogReaderFactoryUI Members

		public object UIControl
		{
			get { return this; }
		}

		public void Apply(IFactoryUICallback hostsFactory)
		{
			StorageAccount account;
			if (devAccountRadioButton.Checked)
				account = new StorageAccount();
			else
				account = new StorageAccount(accountNameTextBox.Text, accountKeyTextBox.Text, useHTTPSCheckBox.Checked);

			IConnectionParams connectParams = factory.CreateParams(account);

			if (hostsFactory.FindExistingProvider(connectParams) != null)
				return;

			ILogProviderHost host = null;
			ILogProvider provider = null;
			try
			{
				host = hostsFactory.CreateHost();
				provider = factory.CreateFromConnectionParams(host, connectParams);
				hostsFactory.AddNewProvider(provider);
			}
			catch
			{
				if (provider != null)
					provider.Dispose();
				if (host != null)
					host.Dispose();
				throw;
			}
		}

		#endregion

		private void devAccountRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			UpdateControls();
		}

		void UpdateControls()
		{
			accountNameTextBox.Enabled = cloudAccountRadioButton.Checked;
			accountKeyTextBox.Enabled = cloudAccountRadioButton.Checked;
			useHTTPSCheckBox.Enabled = cloudAccountRadioButton.Checked;
		}
	}
}
