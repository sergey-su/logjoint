using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using LogJoint.UI.Presenters.SharingDialog;
using System.Runtime.InteropServices;

namespace LogJoint.UI
{
	public partial class ShareDialog : Form, IView
	{
		IViewEvents viewEvents;
		string nameEditBanner;

		public ShareDialog()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents presenter)
		{
			this.viewEvents = presenter;
		}

		void IView.Show()
		{
			ShowDialog();
		}

		void IView.UpdateDescription(string value)
		{
			descriptionLabel.Text = value;
		}

		void IView.UpdateWorkspaceUrlEditBox(string value, bool isHintValue, bool allowCopying)
		{
			urlTextBox.Text = value;
			copyUrlLinkLabel.Enabled = allowCopying;
			urlTextBox.Enabled = !isHintValue;
		}

		void IView.UpdateDialogButtons(bool uploadEnabled, string uploadText, string cancelText)
		{
			uploadButton.Enabled = uploadEnabled;
			uploadButton.Text = uploadText;
			cancelButton.Text = cancelText;
		}

		void IView.UpdateProgressIndicator(string text, bool isError, string details)
		{
			progressIndicatorPanel.Visible = text != null;
			progressLabel.Text = text ?? "";
			progressPictureBox.Visible = !isError;
			errorPictureBox.Visible = isError;
			statusDetailsLink.Visible = !string.IsNullOrEmpty(details);
			statusDetailsLink.Tag = details;
		}

		string IView.GetWorkspaceNameEditValue()
		{
			return nameTextBox.Text;
		}

		string IView.GetWorkspaceAnnotationEditValue()
		{
			return annotationTextBox.Text;
		}

		void IView.UpdateWorkspaceEditControls(bool enabled, string nameValue, string nameBanner, string nameWarning, string annotationValue)
		{
			nameTextBox.Enabled = enabled;
			if (nameTextBox.Text != nameValue)
				nameTextBox.Text = nameValue;
			nameEditBanner = nameBanner;
			UpdateEditBanners();
			annotationTextBox.Enabled = enabled;
			if (annotationTextBox.Text != annotationValue)
				annotationTextBox.Text = annotationValue;
			nameWarningPictureBox.Visible = nameWarning != null;
			toolTip.SetToolTip(nameWarningPictureBox, nameWarning ?? "");
		}

		bool IView.ShowUploadWarningDialog(string message)
		{
			return MessageBox.Show(message, "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes;
		}

		private void uploadButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnUploadButtonClicked();
		}

		private void copyUrlLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (urlTextBox.Text != "")
				Clipboard.SetText(urlTextBox.Text);
		}

		private void ShareDialog_Shown(object sender, EventArgs e)
		{
			UpdateEditBanners();
			if (nameTextBox.CanFocus)
				nameTextBox.Focus();
		}

		void UpdateEditBanners()
		{
			int EM_SETCUEBANNER = 0x1501;
			//if (nameEditBanner != null)
			//	SendMessage(nameTextBox.Handle, EM_SETCUEBANNER, 1, nameEditBanner);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		private void statusDetailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var msg = statusDetailsLink.Tag as string;
			if (!string.IsNullOrEmpty(msg))
				MessageBox.Show(msg, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}