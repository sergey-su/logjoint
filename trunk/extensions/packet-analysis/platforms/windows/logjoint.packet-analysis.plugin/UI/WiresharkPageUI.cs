using System;
using System.Windows.Forms;

namespace LogJoint.PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage
{
	public partial class WiresharkPageUI : UserControl, IView
	{
		public WiresharkPageUI()
		{
			InitializeComponent();
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			browseFileDialog.Filter = "*.*|*.*";

			if (browseFileDialog.ShowDialog() == DialogResult.OK)
				(sender == browseKeyFileButton ? keyFilePathTextBox : filePathTextBox).Text = browseFileDialog.FileName;
		}

		object IView.PageView
		{
			get { return this; }
		}

		string IView.PcapFileNameValue
		{
			get { return filePathTextBox.Text; }
			set { filePathTextBox.Text = value; }
		}

		string IView.KeyFileNameValue
		{
			get { return keyFilePathTextBox.Text; }
			set { keyFilePathTextBox.Text = value; }
		}

		void IView.SetError(string errorOrNull)
		{
			errorLabel.Visible = errorOrNull != null;
			errorLabel.Text = errorOrNull ?? "";
			if (errorOrNull != null)
				errorLabel.BringToFront();
		}

		bool TryGetFile(IDataObject obj, out string fileName)
		{
			fileName = (obj.GetData(DataFormats.FileDrop) as string[])?.FirstOrDefault(null);
			return fileName != null;
		}

		private void filePathTextBox_DragEnter(object sender, DragEventArgs e)
		{
			if (TryGetFile(e.Data, out var _))
				e.Effect = DragDropEffects.Copy;
		}

		private void filePathTextBox_DragOver(object sender, DragEventArgs e)
		{
			if (TryGetFile(e.Data, out var _))
				e.Effect = DragDropEffects.Copy;
		}

		private void filePathTextBox_DragDrop(object sender, DragEventArgs e)
		{
			if (TryGetFile(e.Data, out var fname))
				((TextBox)sender).Text = fname;
		}
	}
}
