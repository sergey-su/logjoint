using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchResult;
using ColoringMode = LogJoint.UI.Presenters.LogViewer.ColoringMode;

namespace LogJoint.UI
{
	public partial class SearchResultView : UserControl, IView
	{
		Presenter presenter;

		public SearchResultView()
		{
			InitializeComponent();
			toolStrip1.ResizingEnabled = true;
			toolStrip1.ResizingStarted += (sender, args) => presenter.ResizingStarted();
			toolStrip1.ResizingFinished += (sender, args) => presenter.ResizingFinished();
			toolStrip1.Resizing += (sender, args) => presenter.Resizing(args.Delta);
		}

		void IView.SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;
		}

		Presenters.LogViewer.IView IView.MessagesView { get { return searchResultViewer; } }
		void IView.SetSearchResultText(string value) { searchResultLabel.Text = value; }
		void IView.SetSearchCompletionPercentage(int value) { searchProgressBar.Value = value; }
		void IView.SetSearchStatusText(string value) { searchStatusLabel.Text = value; }
		void IView.SetSearchProgressBarVisiblity(bool value) { searchProgressBar.Visible = value; }
		void IView.SetSearchStatusLabelVisibility(bool value) { searchStatusLabel.Visible = value; }
		void IView.SetRawViewButtonState(bool visible, bool checked_) { rawViewToolStripButton.Visible = visible; rawViewToolStripButton.Checked = checked_; }
		bool IView.IsMessagesViewFocused { get { return searchResultViewer.Focused; } }
		void IView.SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked)
		{
			coloringNoneMenuItem.Checked = noColoringChecked;
			coloringSourcesMenuItem.Checked = sourcesColoringChecked;
			coloringThreadsMenuItem.Checked = threadsColoringChecked;
		}

		private void closeSearchResultButton_Click(object sender, EventArgs e)
		{
			presenter.CloseSearchResults();
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			presenter.ToggleBookmark();
		}

		private void findCurrentTimeButton_Click(object sender, EventArgs e)
		{
			presenter.FindCurrentTime();
		}

		private void refreshToolStripButton_Click(object sender, EventArgs e)
		{
			presenter.Refresh();
		}

		private void rawViewToolStripButton_Click(object sender, EventArgs e)
		{
			presenter.ToggleRawView();
		}

		private void ColoringMenuItemClicked(object sender, EventArgs e)
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

		private void toolStrip1_Paint(object sender, PaintEventArgs e)
		{

		}

		private void toolStrip1_MouseDown(object sender, MouseEventArgs e)
		{

		}

		private void toolStrip1_MouseEnter(object sender, EventArgs e)
		{

		}

		private void toolStrip1_MouseLeave(object sender, EventArgs e)
		{

		}

		private void toolStrip1_MouseMove(object sender, MouseEventArgs e)
		{

		}
	}
}
