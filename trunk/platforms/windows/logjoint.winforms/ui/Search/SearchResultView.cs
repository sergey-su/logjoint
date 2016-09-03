using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchResult;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;

namespace LogJoint.UI
{
	public partial class SearchResultView : UserControl, IView
	{
		IViewEvents events;

		public SearchResultView()
		{
			InitializeComponent();

			searchResultLabel.Width = UIUtils.Dpi.ScaleUp(100, 120);

			toolStrip1.ImageScalingSize = new Size(UIUtils.Dpi.Scale(16), UIUtils.Dpi.Scale(16));
			toolStrip1.ResizingEnabled = true;
			toolStrip1.ResizingStarted += (sender, args) => events.OnResizingStarted();
			toolStrip1.ResizingFinished += (sender, args) => events.OnResizingFinished();
			toolStrip1.Resizing += (sender, args) => events.OnResizing(args.Delta);

			findCurrentTimeButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.FindCurrentTime, toolStrip1.ImageScalingSize);
			toggleBookmarkButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.Bookmark, toolStrip1.ImageScalingSize);
			refreshToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.Refresh, toolStrip1.ImageScalingSize);
		}

		void IView.SetEventsHandler(IViewEvents events)
		{
			this.events = events;
		}

		Presenters.LogViewer.IView IView.MessagesView { get { return searchResultViewer; } }
		bool IView.IsMessagesViewFocused { get { return searchResultViewer.Focused; } }
		void IView.FocusMessagesView()
		{
			if (searchResultViewer.CanFocus)
				searchResultViewer.Focus();
		}
		void IView.UpdateItems(IList<ViewItem> items)
		{
		}

		void IView.UpdateExpandedState(bool isExpandable, bool isExpanded)
		{
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
	}
}
