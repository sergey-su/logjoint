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
		IPresenter presenter;

		public LoadedMessagesControl()
		{
			InitializeComponent();
		}

		Presenters.LogViewer.IView Presenters.LoadedMessages.IView.MessagesView
		{
			get { return logViewerControl; }
		}

		void IView.SetPresenter(IPresenter presenter)
		{
			this.presenter = presenter;
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
			presenter.ToggleRawView();
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
			presenter.ColoringButtonClicked(coloring);
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			presenter.ToggleBookmark();
		}
	}
}
