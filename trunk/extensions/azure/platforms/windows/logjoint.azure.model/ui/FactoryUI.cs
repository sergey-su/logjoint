using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using LogJoint.MRU;
using LogJoint.Azure;

namespace LogJoint.UI.Azure
{
	public partial class FactoryUI : UserControl, ILogProviderUI
	{
		Factory factory;
		bool updateLocked;
		LJTraceSource trace;
		IRecentlyUsedEntities recentlyUsedLogs;

		public FactoryUI(Factory factory, IRecentlyUsedEntities recentlyUsedLogs)
		{
			this.trace = new LJTraceSource("UI");
			this.factory = factory;
			this.recentlyUsedLogs = recentlyUsedLogs;
			InitializeComponent();
			
			var mostRecentConnectionParams = FindMostRecentConnectionParams();
			if (mostRecentConnectionParams != null)
			{
				PrefillAccountFields(mostRecentConnectionParams);
			}
			
			SetInitialDatesRange();
			UpdateControls();
		}


		private void SetInitialDatesRange()
		{
			var now = DateTime.UtcNow;
			tillDateTimePicker.Value = now;
			fromDateTimePicker.Value = now.AddHours(-1);
			recentPeriodUnitComboBox.SelectedIndex = 1;
		}
		
		Control ILogProviderUI.UIControl
		{
			get { return this; }
		}

		void ILogProviderUI.Apply(IModel model)
		{
			StorageAccount account = CreateStorageAccount();

			IConnectionParams connectParams = null;
			if (loadFixedRangeRadioButton.Checked)
				connectParams = factory.CreateParams(account, fromDateTimePicker.Value, tillDateTimePicker.Value);
			else if (loadRecentRadioButton.Checked)
				connectParams = factory.CreateParams(account, GetRecentPeriod(), liveLogCheckBox.Checked);
			else
				return;

			model.CreateLogSource(factory, connectParams);
		}

		StorageAccount CreateStorageAccount()
		{
			if (devAccountRadioButton.Checked)
				return new StorageAccount();
			else
				return new StorageAccount(accountNameTextBox.Text, accountKeyTextBox.Text, useHTTPSCheckBox.Checked);
		}

		private void devAccountRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			UpdateControls();
		}

		void UpdateControls()
		{
			accountNameTextBox.Enabled = cloudAccountRadioButton.Checked;
			accountKeyTextBox.Enabled = cloudAccountRadioButton.Checked;
			useHTTPSCheckBox.Enabled = cloudAccountRadioButton.Checked;
			fromDateTimePicker.Enabled = loadFixedRangeRadioButton.Checked;
			tillDateTimePicker.Enabled = loadFixedRangeRadioButton.Checked;
			recentPeriodCounter.Enabled = loadRecentRadioButton.Checked;
			recentPeriodUnitComboBox.Enabled = loadRecentRadioButton.Checked;
			liveLogCheckBox.Enabled = false;// loadRecentRadioButton.Checked; TODO: implement auto updates
		}

		private void fromDateTimePicker_ValueChanged(object sender, EventArgs e)
		{
			if (!updateLocked)
				using (LockControlUpdates())
					tillDateTimePicker.MinDate = fromDateTimePicker.Value;
		}

		private void tillDateTimePicker_ValueChanged(object sender, EventArgs e)
		{
			if (!updateLocked)
				using (LockControlUpdates())
					fromDateTimePicker.MaxDate = tillDateTimePicker.Value;
		}

		IDisposable LockControlUpdates()
		{
			return new ScopedGuard(() => { updateLocked = true; }, () => { updateLocked = false; });
		}

		private void loadFixedRangeRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if (!updateLocked)
			{
				using (LockControlUpdates())
					loadRecentRadioButton.Checked = false;
				UpdateControls();
			}
		}

		private void loadRecentRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if (!updateLocked)
			{
				using (LockControlUpdates())
					loadFixedRangeRadioButton.Checked = false;
				UpdateControls();
			}
		}

		TimeSpan GetRecentPeriod()
		{
			var counter = recentPeriodCounter.Value;
			TimeSpan[] units = new TimeSpan[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1),
				TimeSpan.FromDays(7),
				TimeSpan.FromDays(30),
				TimeSpan.FromDays(365)
			};
			var unit = units[recentPeriodUnitComboBox.SelectedIndex];
			return new TimeSpan(-counter * unit.Ticks);
		}

		private void testConnectionButton_Click(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
			try
			{
				factory.TestAccount(CreateStorageAccount());
				MessageBox.Show("Your account is OK");
			}
			catch (Exception ex)
			{
				trace.Error(ex, "Storage account test failed");
				MessageBox.Show(string.Format("Failed to connect to storage account:\n{0}", ex.Message), "Testing your account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		AzureConnectionParams FindMostRecentConnectionParams()
		{
			var serializedParams =
				recentlyUsedLogs.GetMRUList().OfType<RecentLogEntry>().Where(e => e.Factory == factory).Select(e => e.ConnectionParams).FirstOrDefault();
			if (serializedParams == null)
				return null;
			try
			{
				return new AzureConnectionParams(serializedParams);
			}
			catch (InvalidConnectionParamsException)
			{
				return null;
			}
		}

		void PrefillAccountFields(AzureConnectionParams cp)
		{
			if (cp.Account.AccountType == StorageAccount.Type.CloudAccount)
			{
				devAccountRadioButton.Checked = false;
				cloudAccountRadioButton.Checked = true;
				accountNameTextBox.Text = cp.Account.AccountName;
				accountKeyTextBox.Text = cp.Account.AccountKey;
				useHTTPSCheckBox.Checked = cp.Account.UseHTPPS;
			}
			else
			{
				devAccountRadioButton.Checked = true;
				cloudAccountRadioButton.Checked = false;
			}
		}
	}
}
