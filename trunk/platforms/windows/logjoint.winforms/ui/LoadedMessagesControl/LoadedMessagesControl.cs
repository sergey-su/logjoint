using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.LoadedMessages;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;

namespace LogJoint.UI
{
	public partial class LoadedMessagesControl : UserControl, IView
	{
		IViewEvents eventsHandler;

		public LoadedMessagesControl()
		{
			InitializeComponent();

			toolStrip1.ImageScalingSize = new Size(UIUtils.Dpi.Scale(14), UIUtils.Dpi.Scale(14));
			toggleBookmarkButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.Bookmark, toolStrip1.ImageScalingSize);
			rawViewToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.RawView, toolStrip1.ImageScalingSize);

			toolStrip1.ResizingEnabled = true;
			toolStrip1.ResizingStarted += (sender, args) => eventsHandler.OnResizingStarted();
			toolStrip1.ResizingFinished += (sender, args) => eventsHandler.OnResizingFinished();
			toolStrip1.Resizing += (sender, args) => eventsHandler.OnResizing(args.Delta);
		}

		Presenters.LogViewer.IView Presenters.LoadedMessages.IView.MessagesView
		{
			get { return logViewerControl; }
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetRawViewButtonState(bool visible, bool checked_)
		{
			rawViewToolStripButton.Visible = visible;
			rawViewToolStripButton.Checked = checked_; 
		}

		void IView.SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked)
		{
			coloringNoneMenuItem.Checked = noColoringChecked;
			coloringSourcesMenuItem.Checked = sourcesColoringChecked;
			coloringThreadsMenuItem.Checked = threadsColoringChecked;
		}

		void IView.Focus()
		{
			if (base.CanFocus)
				base.Focus();
		}

		private void rawViewToolStripButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnToggleRawView();
		}

		private void coloringMenuItem_Click(object sender, EventArgs e)
		{
			ColoringMode coloring;
			if (sender == coloringNoneMenuItem)
				coloring = ColoringMode.None;
			else if (sender == coloringThreadsMenuItem)
				coloring = ColoringMode.Threads;
			else if (sender == coloringSourcesMenuItem)
				coloring = ColoringMode.Sources;
			else
				return;
			eventsHandler.OnColoringButtonClicked(coloring);
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnToggleBookmark();
		}
	}
}
