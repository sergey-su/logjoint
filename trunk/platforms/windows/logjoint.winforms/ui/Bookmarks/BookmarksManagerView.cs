using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.BookmarksManager;

namespace LogJoint.UI
{
	public partial class BookmarksManagerView : UserControl, IView
	{
		IViewEvents presenter;

		public BookmarksManagerView()
		{
			InitializeComponent();
		}

		public BookmarksView ListView { get { return bookmarksView; } }

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		bool IView.ShowDeleteConfirmationPopup(int nrOfBookmarks)
		{
			return MessageBox.Show(
				string.Format("You are about to delete ({0}) bookmark(s).\nAre you sure?", nrOfBookmarks),
					"LogJoint", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes;
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			presenter.OnToggleButtonClicked();
		}

		private void deleteAllBookmarksButton_Click(object sender, EventArgs e)
		{
			presenter.OnDeleteAllButtonClicked();
		}

		private void nextBookmarkButton_Click(object sender, EventArgs e)
		{
			presenter.OnNextBmkButtonClicked();
		}

		private void prevBookmarkButton_Click(object sender, EventArgs e)
		{
			presenter.OnPrevBmkButtonClicked();
		}
	}
}
