using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.NewLogSourceDialog;
using LogJoint.MRU;

namespace LogJoint.UI
{
	public partial class NewLogSourceDialog : Form, IDialog
	{
		LogTypeEntry current;
		IRecentlyUsedEntities mru;
		LogJoint.UI.Presenters.MainForm.ICommandLineHandler commandLineHandler;
		IModel model;
		Presenters.Help.IPresenter help;
		UI.ILogProviderUIsRegistry registry;

		abstract class LogTypeEntry: IDisposable
		{
			public ILogProviderUI UI;

			public abstract object GetIdentityObject();
			public abstract string GetDescription();
			public abstract ILogProviderUI CreateUI(IModel model);

			public void Dispose()
			{
				if (UI != null)
					UI.Dispose();
			}
		};

		class FixedLogTypeEntry : LogTypeEntry
		{
			public override string ToString() { return LogProviderFactoryRegistry.ToString(Factory); }

			public override object GetIdentityObject() { return Factory; }

			public override string GetDescription() { return Factory.FormatDescription; }

			public override ILogProviderUI CreateUI(IModel model) { return UIsRegistry.CreateProviderUI(Factory); }

			public ILogProviderUIsRegistry UIsRegistry;
			public ILogProviderFactory Factory;
		};

		class AutodetectedLogTypeEntry : LogTypeEntry
		{
			public LogJoint.UI.Presenters.MainForm.ICommandLineHandler commandLineHandler;

			public override string ToString() { return name; }

			public override object GetIdentityObject() { return name; }

			public override string GetDescription() { return "Pick a file or URL and LogJoint will detect log format by trying all known formats"; }

			public override ILogProviderUI CreateUI(IModel model)
			{ return new AnyLogFormatUI(commandLineHandler); }

			private static string name = "Any known log format";
		};

		public NewLogSourceDialog(
			IModel model, 
			LogJoint.UI.Presenters.MainForm.ICommandLineHandler commandLineHandler, 
			Presenters.Help.IPresenter help,
			UI.ILogProviderUIsRegistry registry
		)
		{
			InitializeComponent();

			this.model = model;
			this.mru = model.MRU;
			this.commandLineHandler = commandLineHandler;
			this.help = help;
			this.registry = registry;

			formatNameLabel.Text = "";
		}

		void IDialog.Show()
		{
			Execute();
		}

		void UpdateList()
		{
			logTypeListBox.BeginUpdate();
			try
			{
				object oldSelection = current != null ? current.GetIdentityObject() : null;
				SetCurrent(null);

				logTypeListBox.Items.Clear();
				logTypeListBox.Items.Add(new AutodetectedLogTypeEntry() { 
					commandLineHandler = this.commandLineHandler
				});
				foreach (ILogProviderFactory fact in mru.SortFactoriesMoreRecentFirst(model.LogProviderFactoryRegistry.Items))
				{
					FixedLogTypeEntry entry = new FixedLogTypeEntry();
					entry.Factory = fact;
					entry.UIsRegistry = registry;
					logTypeListBox.Items.Add(entry);
				}

				int newSelectedIdx = 0;
				if (oldSelection != null)
				{
					for (int i = 0; i < logTypeListBox.Items.Count; ++i)
					{
						if (Get(i).GetIdentityObject() == oldSelection)
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

		LogTypeEntry Get(int idx)
		{
			return (LogTypeEntry)logTypeListBox.Items[idx];
		}

		LogTypeEntry GetSelected()
		{
			if (logTypeListBox.SelectedIndex >= 0)
				return Get(logTypeListBox.SelectedIndex);
			return null;
		}

		void SetCurrent(LogTypeEntry entry)
		{
			LogTypeEntry tmp = entry;

			if (tmp == current)
				return;

			if (current != null)
			{
				if (current.UI != null)
					current.UI.UIControl.Visible = false;
			}
			current = tmp;
			if (current != null)
			{
				this.formatNameLabel.Text = current.ToString();
				this.formatDescriptionLabel.Text = current.GetDescription();
				ILogProviderUI ui = current.UI;
				if (current.UI == null)
				{
					ui = current.UI = current.CreateUI(model);
				}
				if (current.UI != null)
				{
					Control ctrl = ui.UIControl;
					ctrl.Parent = this.hostPanel;
					ctrl.Dock = DockStyle.Fill;
					ctrl.Visible = true;
				}
			}
		}

		private void logTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			LogTypeEntry tmp = GetSelected();
			SetCurrent(tmp);
		}

		bool Apply()
		{
			// todo: handle errors
			if (current.UI != null)
				current.UI.Apply(model);
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
		}

		private void NewLogSourceDialog_Shown(object sender, EventArgs e)
		{
			if (current != null && current.UI != null)
			{
				var ctrl = current.UI.UIControl;
				if (ctrl != null && ctrl.CanFocus)
					ctrl.Focus();
			}
		}
	}

	public class NewLogSourceDialogView : IView
	{
		IModel model;
		LogJoint.UI.Presenters.MainForm.ICommandLineHandler commandLineHandler;
		Presenters.Help.IPresenter helpPresenters;
		UI.ILogProviderUIsRegistry registry;

		public NewLogSourceDialogView(
			IModel model, 
			LogJoint.UI.Presenters.MainForm.ICommandLineHandler commandLineHandler, 
			Presenters.Help.IPresenter helpPresenters,
			UI.ILogProviderUIsRegistry registry
		)
		{
			this.model = model;
			this.commandLineHandler = commandLineHandler;
			this.helpPresenters = helpPresenters;
			this.registry = registry;
		}

		IDialog IView.CreateDialog()
		{
			return new NewLogSourceDialog(model, commandLineHandler, helpPresenters, registry);
		}
	};
}