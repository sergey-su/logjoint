using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcePropertiesWindow;

namespace LogJoint.UI
{
	public partial class SourceDetailsForm : Form, IWindow
	{
		readonly IViewEvents viewEvents;
		readonly Dictionary<ControlFlag, Control> controls = new Dictionary<ControlFlag, Control>();

		public SourceDetailsForm(IViewEvents viewEvents)
		{
			InitializeComponent();

			this.viewEvents = viewEvents;

			controls[ControlFlag.NameEditbox] = nameTextBox;
			controls[ControlFlag.FormatTextBox] = formatTextBox;
			controls[ControlFlag.VisibleCheckBox] = visibleCheckBox;
			controls[ControlFlag.ColorPanel] = colorPanel;
			controls[ControlFlag.StateDetailsLink] = stateDetailsLink;
			controls[ControlFlag.StateLabel] = stateLabel;
			controls[ControlFlag.LoadedMessagesTextBox] = loadedMessagesTextBox;
			controls[ControlFlag.LoadedMessagesWarningIcon] = loadedMessagesWarningIcon;
			controls[ControlFlag.LoadedMessagesWarningLinkLabel] = loadedMessagesWarningLinkLabel;
			controls[ControlFlag.TrackChangesLabel] = trackChangesLabel;
			controls[ControlFlag.SuspendResumeTrackingLink] = suspendResumeTrackingLink;
			controls[ControlFlag.FirstMessageLinkLabel] = firstMessageLinkLabel;
			controls[ControlFlag.LastMessageLinkLabel] = lastMessageLinkLabel;
			controls[ControlFlag.SaveAsButton] = saveAsButton;
			controls[ControlFlag.AnnotationTextBox] = annotationTextBox;
			controls[ControlFlag.TimeOffsetTextBox] = timeOffsetTextBox;
			controls[ControlFlag.CopyPathButton] = copyPathLink;
		}

		void IWindow.ShowDialog()
		{
			if (!IsDisposed)
				this.ShowDialog();
		}

		void IWindow.WriteControl(ControlFlag flags, string value)
		{
			Control ctrl;
			if (!controls.TryGetValue(flags & ControlFlag.ControlIdMask, out ctrl))
				return;
			if ((flags & ControlFlag.Value) != 0)
			{
				ctrl.Text = value;
				if (ctrl is TextBox)
					(ctrl as TextBox).Select(0, 0);
			}
			else if ((flags & ControlFlag.Checked) != 0)
			{
				var cb = ctrl as CheckBox;
				if (cb != null)
					cb.Checked = value != null;
			}
			else if ((flags & ControlFlag.Visibility) != 0)
				ctrl.Visible = value != null;
			else if ((flags & ControlFlag.BackColor) != 0)
				ctrl.BackColor = new ModelColor(uint.Parse(value)).ToColor();
			else if ((flags & ControlFlag.ForeColor) != 0)
				ctrl.ForeColor = new ModelColor(uint.Parse(value)).ToColor();
			else if ((flags & ControlFlag.Enabled) != 0)
				ctrl.Enabled = value != null;
		}

		string IWindow.ReadControl(ControlFlag flags)
		{
			Control ctrl;
			if (!controls.TryGetValue(flags & ControlFlag.ControlIdMask, out ctrl))
				return null;
			if ((flags & ControlFlag.Value) != 0)
				return ctrl.Text;
			else if ((flags & ControlFlag.Checked) != 0)
				return ctrl is CheckBox && (ctrl as CheckBox).Checked ? "" : null;
			else
				return null;
		}

		void IWindow.ShowColorSelector(ModelColor[] options)
		{
			var menu = new ContextMenuStrip();
			foreach (var cl in options)
			{
				var mi = new ToolStripMenuItem()
				{
					DisplayStyle = ToolStripItemDisplayStyle.None,
					BackColor = cl.ToColor(),
					AutoSize = false,
					Size = new Size(300, (int)UIUtils.Dpi.Scale(15f))
				};
				mi.Paint += colorOptionMenuItemPaint;
				mi.Click += (s, e) => viewEvents.OnColorSelected(cl);
				menu.Items.Add(mi);
			}
			menu.Show(changeColorLinkLabel, new Point(0, changeColorLinkLabel.Height));
		}

		private void colorOptionMenuItemPaint(object sender, PaintEventArgs e)
		{
			var mi = sender as ToolStripMenuItem;
			if (mi == null)
				return;
			using (var b = new SolidBrush(mi.BackColor))
				e.Graphics.FillRectangle(b, e.ClipRectangle);
			e.Graphics.DrawLine(Pens.LightGray, 0, 0, e.ClipRectangle.Right, 0);
		}

		private void visibleCheckBox_Click(object sender, EventArgs e)
		{
			viewEvents.OnVisibleCheckBoxClicked();
		}

		private void suspendResumeTrackingLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			viewEvents.OnSuspendResumeTrackingLinkClicked();
		}

		private void stateDetailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			viewEvents.OnStateDetailsLinkClicked();
		}

		private void bookmarkClicked(object sender, EventArgs e)
		{
			var ctrl = controls.Where(c => (object)c.Value == sender).FirstOrDefault();
			if (ctrl.Value != null)
				viewEvents.OnBookmarkLinkClicked(ctrl.Key);
		}

		private void saveAsButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnSaveAsButtonClicked();
		}

		private void SourceDetailsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			viewEvents.OnClosingDialog();
		}

		private void loadedMessagesWarningIcon_Click(object sender, EventArgs e)
		{
			viewEvents.OnLoadedMessagesWarningIconClicked();
		}

		void changeColorLinkLabel_Click(object sender, System.EventArgs e)
		{
			viewEvents.OnChangeColorLinkClicked();
		}

		void copyPathLink_LinkClicked(object sender, System.EventArgs e)
		{
			viewEvents.OnCopyButtonClicked();
		}
	}

	public class SourceDetailsWindowView : IView
	{
		IViewEvents viewEvents;

		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		IWindow IView.CreateWindow()
		{
			return new SourceDetailsForm(viewEvents);
		}

		uint IView.DefaultControlForeColor
		{
			get { return new ModelColor(SystemColors.ControlText.ToArgb()).Argb; }
		}
	};
}