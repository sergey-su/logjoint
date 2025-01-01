using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.LoadedMessages;

namespace LogJoint.UI
{
    public partial class LoadedMessagesControl : UserControl
    {
        IViewModel viewModel;

        public LoadedMessagesControl()
        {
            InitializeComponent();

            toolStrip1.ImageScalingSize = new Size(UIUtils.Dpi.Scale(14), UIUtils.Dpi.Scale(14));
            toggleBookmarkButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.Bookmark, toolStrip1.ImageScalingSize);
            rawViewToolStripButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.RawView, toolStrip1.ImageScalingSize);

            toolStrip1.ResizingEnabled = true;
            toolStrip1.ResizingStarted += (sender, args) => viewModel.OnResizingStarted();
            toolStrip1.ResizingFinished += (sender, args) => viewModel.OnResizingFinished();
            toolStrip1.Resizing += (sender, args) => viewModel.OnResizing(args.Delta);
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
            logViewerControl.SetViewModel(viewModel.LogViewer);

            var updateView = Updaters.Create(
                () => viewModel.ViewState,
                state =>
                {
                    toggleBookmarkButton.Visible = state.ToggleBookmark.Visible;
                    toggleBookmarkButton.ToolTipText = state.ToggleBookmark.Tooltip;
                    rawViewToolStripButton.Visible = state.RawViewButton.Visible;
                    rawViewToolStripButton.Checked = state.RawViewButton.Checked;
                    rawViewToolStripButton.ToolTipText = state.RawViewButton.Tooltip;
                    viewTailToolStripButton.Checked = state.ViewTailButton.Checked;
                    viewTailToolStripButton.Visible = state.ViewTailButton.Visible;
                    viewTailToolStripButton.ToolTipText = state.ViewTailButton.Tooltip;
                    busyIndicatorLabel.Visible = state.NavigationProgressIndicator.Visible;
                    busyIndicatorLabel.ToolTipText = state.NavigationProgressIndicator.Tooltip;
                    coloringDropDownButton.Visible = state.Coloring.Visible;
                    foreach (var i in (new[] { coloringMenuItem1, coloringMenuItem2, coloringMenuItem3 })
                        .Select((option, idx) => (option, idx)))
                    {
                        i.option.Text = state.Coloring.Options[i.idx].Text;
                        i.option.ToolTipText = state.Coloring.Options[i.idx].Tooltip;
                        i.option.Tag = i.idx;
                        i.option.Checked = i.idx == state.Coloring.Selected;
                    }
                }
            );

            viewModel.ChangeNotification.CreateSubscription(updateView);
        }

        private void rawViewToolStripButton_Click(object sender, EventArgs e)
        {
            viewModel.OnToggleRawView();
        }

        private void viewTailToolStripButton_Click(object sender, EventArgs e)
        {
            viewModel.OnToggleViewTail();
        }

        private void coloringMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is int modeIdx)
            {
                viewModel.OnColoringButtonClicked(modeIdx);
            }
        }

        private void toggleBookmarkButton_Click(object sender, EventArgs e)
        {
            viewModel.OnToggleBookmark();
        }
    }
}
