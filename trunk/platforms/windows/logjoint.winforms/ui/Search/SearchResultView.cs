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
		IViewEvents events;

		public SearchResultView()
		{
			InitializeComponent();
			toolStrip1.ResizingEnabled = true;
			toolStrip1.ResizingStarted += (sender, args) => events.OnResizingStarted();
			toolStrip1.ResizingFinished += (sender, args) => events.OnResizingFinished();
			toolStrip1.Resizing += (sender, args) => events.OnResizing(args.Delta);
		}

		void IView.SetEventsHandler(IViewEvents events)
		{
			this.events = events;
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
			events.OnCloseSearchResultsButtonClicked();
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			events.OnToggleBookmarkButtonClicked();
		}

		private void findCurrentTimeButton_Click(object sender, EventArgs e)
		{
			events.OnFindCurrentTimeButtonClicked();
		}

		private void refreshToolStripButton_Click(object sender, EventArgs e)
		{
			events.OnRefreshButtonClicked();
		}

		private void rawViewToolStripButton_Click(object sender, EventArgs e)
		{
			events.OnToggleRawViewButtonClicked();
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
			events.OnColoringButtonClicked(coloring);
		}
	}
}
