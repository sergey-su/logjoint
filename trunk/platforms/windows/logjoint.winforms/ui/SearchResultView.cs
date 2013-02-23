using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchResult;

namespace LogJoint.UI
{
	public partial class SearchResultView : UserControl, IView
	{
		Presenter presenter;

		public SearchResultView()
		{
			InitializeComponent();
		}

		public void SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;
		}

		public Presenters.LogViewer.IView MessagesView { get { return searchResultViewer; } }
		public void SetSearchResultText(string value) { searchResultLabel.Text = value; }
		public void SetSearchCompletionPercentage(int value) { searchProgressBar.Value = value; }
		public void SetSearchStatusText(string value) { searchStatusLabel.Text = value; }
		public void SetSearchProgressBarVisiblity(bool value) { searchProgressBar.Visible = value; }
		public void SetSearchStatusLabelVisibility(bool value) { searchStatusLabel.Visible = value; }
		public bool IsMessagesViewFocused { get { return searchResultViewer.Focused; } }

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
	}
}
