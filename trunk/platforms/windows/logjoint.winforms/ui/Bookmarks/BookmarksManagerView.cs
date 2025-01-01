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
    public partial class BookmarksManagerView : UserControl
    {
        IViewModel viewModel;

        public BookmarksManagerView()
        {
            InitializeComponent();
        }

        public BookmarksView ListView { get { return bookmarksView; } }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        private void toggleBookmarkButton_Click(object sender, EventArgs e)
        {
            viewModel.OnToggleButtonClicked();
        }

        private void deleteAllBookmarksButton_Click(object sender, EventArgs e)
        {
            viewModel.OnDeleteAllButtonClicked();
        }

        private void nextBookmarkButton_Click(object sender, EventArgs e)
        {
            viewModel.OnNextBmkButtonClicked();
        }

        private void prevBookmarkButton_Click(object sender, EventArgs e)
        {
            viewModel.OnPrevBmkButtonClicked();
        }
    }
}
