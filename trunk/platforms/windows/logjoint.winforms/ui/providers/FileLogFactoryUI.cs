using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat
{
	public partial class FileLogFactoryUI : UserControl, IView
	{
		IViewEvents eventsHandler;

		public FileLogFactoryUI()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		object IView.PageView
		{
			get { return this; }
		}

		object IView.ReadControlValue(ControlId id)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					return independentLogModeRadioButton.Checked;
				case ControlId.RotatedLogModeButton:
					return rotatedLogModeRadioButton.Checked;
				case ControlId.FileSelector:
					return filePathTextBox.Text;
				case ControlId.FolderSelector:
					return folderPartTextBox.Text;
				case ControlId.PatternsSelector:
					return folderPatternsTextBox.Text;
			}
			return null;
		}

		void IView.WriteControlValue(ControlId id, object value)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					independentLogModeRadioButton.Checked = (bool)value; 
					break;
				case ControlId.RotatedLogModeButton:
					rotatedLogModeRadioButton.Checked = (bool)value;
					break;
				case ControlId.FileSelector:
					filePathTextBox.Text = (string)value;
					break;
				case ControlId.FolderSelector:
					folderPartTextBox.Text = (string)value;
					break;
				case ControlId.PatternsSelector:
					folderPatternsTextBox.Text = (string)value;
					break;
			}
		}

		void IView.SetEnabled(ControlId id, bool value)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					independentLogModeRadioButton.Enabled = value;
					break;
				case ControlId.RotatedLogModeButton:
					rotatedLogModeRadioButton.Enabled = value;
					break;
				case ControlId.FileSelector:
					filePathTextBox.Enabled = value;
					browseFileButton.Enabled = value;
					break;
				case ControlId.FolderSelector:
					folderPartTextBox.Enabled = value;
					browseFolderButton.Enabled = value;
					break;
				case ControlId.PatternsSelector:
					folderPatternsTextBox.Enabled = value;
					break;
			}
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnBrowseFilesButtonClicked();
		}

		private void RadioButtonCheckedChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSelectedModeChanged();
		}

		private void browseFolderButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnBrowseFolderButtonClicked();
		}
	}
}
