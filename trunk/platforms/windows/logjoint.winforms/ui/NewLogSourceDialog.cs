using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class NewLogSourceDialog : Form
	{
		FactoryEntry current;
		IFactoryUICallback callback;
		IRecentlyUsedLogs mru;

		class FactoryEntry: IDisposable
		{
			public ILogProviderFactory Factory;
			public ILogProviderFactoryUI UI;
			public override string ToString()
			{
				return LogProviderFactoryRegistry.ToString(Factory);
			}

			public void Dispose()
			{
				if (UI != null)
					UI.Dispose();
			}
		};

		public NewLogSourceDialog(IFactoryUICallback callback, IRecentlyUsedLogs mru)
		{
			InitializeComponent();

			this.callback = callback;
			this.mru = mru;

			formatNameLabel.Text = "";
		}

		void UpdateList()
		{
			logTypeListBox.BeginUpdate();
			try
			{
				ILogProviderFactory oldSelection = current != null ? current.Factory : null;
				SetCurrent(null);

				logTypeListBox.Items.Clear();
				foreach (ILogProviderFactory fact in mru.SortFactoriesMoreRecentFirst(LogProviderFactoryRegistry.DefaultInstance.Items))
				{
					FactoryEntry entry = new FactoryEntry();
					entry.Factory = fact;
					logTypeListBox.Items.Add(entry);
				}

				int newSelectedIdx = 0;
				if (oldSelection != null)
				{
					for (int i = 0; i < logTypeListBox.Items.Count; ++i)
					{
						if (Get(i).Factory == oldSelection)
						{
							newSelectedIdx = i;
							break;
						}
					}
				}

				if (newSelectedIdx < logTypeListBox.Items.Count)
				{
					logTypeListBox.SelectedIndex = newSelectedIdx;
				}
			}
			finally
			{
				logTypeListBox.EndUpdate();
			}
		}

		public void Execute()
		{
			UpdateList();
			ShowDialog();
		}

		FactoryEntry Get(int idx)
		{
			return (FactoryEntry)logTypeListBox.Items[idx];
		}

		FactoryEntry GetSelected()
		{
			if (logTypeListBox.SelectedIndex >= 0)
				return Get(logTypeListBox.SelectedIndex);
			return null;
		}

		void SetCurrent(FactoryEntry entry)
		{
			FactoryEntry tmp = entry;

			if (tmp == current)
				return;

			if (current != null)
			{
				if (current.UI != null)
					(current.UI.UIControl as Control).Visible = false;
			}
			current = tmp;
			if (current != null)
			{
				this.formatNameLabel.Text = current.ToString();
				this.formatDescriptionLabel.Text = current.Factory.FormatDescription;
				ILogProviderFactoryUI ui = current.UI;
				if (current.UI == null)
				{
					ui = current.UI = current.Factory.CreateUI(new UIFactory());
				}
				if (current.UI != null)
				{
					Control ctrl = ui.UIControl as Control;
					ctrl.Parent = this.hostPanel;
					ctrl.Dock = DockStyle.Fill;
					ctrl.Visible = true;
				}
			}
		}

		private void logTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			FactoryEntry tmp = GetSelected();
			SetCurrent(tmp);
		}

		bool Apply()
		{
			// todo: handle errors
			if (current.UI != null)
				current.UI.Apply(callback);
			return true;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			if (Apply())
				this.DialogResult = DialogResult.OK;
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			Apply();
		}

		private void manageFormatsButton_Click(object sender, EventArgs e)
		{
			using (ManageFormatsWizard w = new ManageFormatsWizard())
			{
				w.ExecuteWizard();
			}
			if (UserDefinedFormatsManager.DefaultInstance.ReloadFactories() > 0)
			{
				UpdateList();
			}
		}
	}
}