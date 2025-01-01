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
    public partial class SearchResultView : UserControl
    {
        IViewModel viewModel;
        Image pinImage, pinnedImage, dropdownImage, hideDropdownImage, emptyImage;
        ToolStripControls mainToolStripControls;
        bool updateLock;
        string expandButtonHint;
        string unexpandButtonHint;
        internal SplitContainer SearchResultsSplitContainer;

        public SearchResultView()
        {
            InitializeComponent();


            mainToolStrip.ImageScalingSize = new Size(UIUtils.Dpi.Scale(16), UIUtils.Dpi.Scale(16));
            mainToolStrip.ResizingEnabled = true;
            mainToolStrip.ResizingStarted += (sender, args) => viewModel.OnResizingStarted();
            mainToolStrip.ResizingFinished += (sender, args) => viewModel.OnResizingFinished();
            mainToolStrip.Resizing += (sender, args) => viewModel.OnResizing(args.Delta);

            findCurrentTimeButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.FindCurrentTime, mainToolStrip.ImageScalingSize);
            toggleBookmarkButton.Image = UIUtils.DownscaleUIImage(Properties.Resources.Bookmark, mainToolStrip.ImageScalingSize);
            pinImage = UIUtils.DownscaleUIImage(Properties.Resources.Pin, mainToolStrip.ImageScalingSize);
            pinnedImage = UIUtils.DownscaleUIImage(Properties.Resources.Pinned, mainToolStrip.ImageScalingSize);
            dropdownImage = UIUtils.DownscaleUIImage(Properties.Resources.Dropdown, mainToolStrip.ImageScalingSize);
            hideDropdownImage = UIUtils.DownscaleUIImage(Properties.Resources.HideDropdown, mainToolStrip.ImageScalingSize);
            emptyImage = new Bitmap(mainToolStrip.ImageScalingSize.Width, mainToolStrip.ImageScalingSize.Height);

            dropDownPanel.Left = this.PointToClient(mainToolStrip.PointToScreen(new Point(toggleBookmarkButton.Bounds.Right, 0))).X;
            dropDownPanel.Height = mainToolStrip.Height * 3;

            mainToolStripControls = AddToolStripControls(mainToolStrip);
            mainToolStripControls.dropdownBtnShowsList = true;
            UpdateToolStripControls(mainToolStripControls, null);
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
            searchResultViewer.SetViewModel(viewModel.LogViewer);

            var itemsUpdater = Updaters.Create(
                () => viewModel.Items,
                items =>
                {
                    using (new ScopedGuard(
                        () =>
                        {
                            dropDownPanel.SuspendLayout();
                            updateLock = true;
                        },
                        () =>
                        {
                            dropDownPanel.ResumeLayout();
                            updateLock = false;
                        }
                    ))
                    {
                        UpdateToolStripControls(mainToolStripControls, items.FirstOrDefault());

                        var existingControls = dropDownPanel.Controls.OfType<ExtendedToolStrip>().ToList();

                        int idx = 0;
                        foreach (var item in items)
                        {
                            var row = existingControls.LastOrDefault();
                            if (row != null)
                            {
                                existingControls.RemoveAt(existingControls.Count - 1);
                            }
                            else
                            {
                                row = new ExtendedToolStrip()
                                {
                                    GripStyle = ToolStripGripStyle.Hidden,
                                    ImageScalingSize = mainToolStrip.ImageScalingSize,
                                    Font = mainToolStrip.Font,
                                    TabStop = true,
                                };
                                row.Tag = AddToolStripControls(row);
                                dropDownPanel.Controls.Add(row);
                                dropDownPanel.Controls.SetChildIndex(row, 0);
                            }

                            var ctrls = (ToolStripControls)row.Tag;
                            ctrls.dropdownBtnShowsList = (idx == 0) ? false : new bool?();
                            UpdateToolStripControls(ctrls, item);

                            ++idx;
                        }

                        existingControls.ForEach(c => c.Dispose());

                        UpdateToolStripTextSizes();
                    }
                }
            );
            var updateVisiblity = Updaters.Create(() => viewModel.IsSearchResultsVisible, value =>
            {
                if (SearchResultsSplitContainer != null)
                    SearchResultsSplitContainer.Panel2Collapsed = !value;
            });
            var updateExpansion = Updaters.Create(() => viewModel.ExpansionState, state =>
            {
                UpdateExpandedState(state.IsExpandable, state.IsExpanded, state.PreferredListHeightInRows, state.ExpandButtonHint, state.UnexpandButtonHint);
            });
            viewModel.ChangeNotification.CreateSubscription(() =>
            {
                itemsUpdater();
                updateVisiblity();
                updateExpansion();
            });
        }

        void UpdateExpandedState(bool isExpandable, bool isExpanded, int preferredListHeightInRows, string expandButtonHint, string unexpandButtonHint)
        {
            dropDownPanel.Visible = isExpanded;
            if (isExpanded)
            {
                var preferredHight = mainToolStrip.Height * preferredListHeightInRows;
                dropDownPanel.Height = preferredHight;
                dropDownPanel.Focus();
            }
            mainToolStripControls.dropdownBtn.Enabled = isExpandable;
            this.expandButtonHint = expandButtonHint;
            this.unexpandButtonHint = unexpandButtonHint;
        }

        void UpdateToolStripControls(ToolStripControls ctrls, ViewItem item)
        {
            ctrls.visibleCbHost.Visible = item != null;
            ctrls.pinnedBtn.Visible = item != null;
            ctrls.textLabel.Visible = item != null;
            if (item == null)
                return;
            if (!ctrls.isInitialized)
            {
                ctrls.dropdownBtn.Enabled = ctrls.dropdownBtnShowsList != null;
                ctrls.dropdownBtn.Image =
                    ctrls.dropdownBtnShowsList == null ? emptyImage :
                    ctrls.dropdownBtnShowsList == true ? dropdownImage :
                    hideDropdownImage;
                ctrls.dropdownBtn.ToolTipText =
                    ctrls.dropdownBtnShowsList == null ? "" :
                    ctrls.dropdownBtnShowsList == true ? expandButtonHint :
                    unexpandButtonHint;
                if (ctrls.dropdownBtnShowsList != null)
                    ctrls.dropdownBtn.Click += (s, e) => viewModel.OnExpandSearchesListClicked();
                ctrls.visibleCbHost.ToolTipText = item.VisiblityControlHint;
                ctrls.visibleCb.CheckedChanged += (s, e) => { if (!updateLock) viewModel.OnVisibilityCheckboxClicked(ctrls.currentItem); };
                ctrls.pinnedBtn.ToolTipText = item.PinControlHint;
                ctrls.pinnedBtn.Click += (s, e) => { if (!updateLock) viewModel.OnPinCheckboxClicked(ctrls.currentItem); };
                if (ctrls.dropdownBtnShowsList != null)
                    ctrls.textLabel.Click += (s, e) => viewModel.OnDropdownTextClicked();
                ctrls.toolStrip.ContextMenuStrip.Opening += (s, e) =>
                {
                    var menuData = viewModel.OnContextMenuPopup(ctrls.currentItem);
                    void updateItem(ToolStripMenuItem menuItem, MenuItemId id)
                    {
                        menuItem.Visible = (menuData.VisibleItems & id) != 0;
                        menuItem.Checked = (menuData.CheckedItems & id) != 0;
                    }
                    updateItem(ctrls.visibleMenuItem, MenuItemId.Visible);
                    updateItem(ctrls.pinnedMenuItem, MenuItemId.Pinned);
                    updateItem(ctrls.visibleOnTimelineMenuItem, MenuItemId.VisibleOnTimeline);
                    updateItem(ctrls.deleteSeparatorMenuItem, MenuItemId.Delete);
                    updateItem(ctrls.deleteMenuItem, MenuItemId.Delete);
                };
                ctrls.isInitialized = true;
            }
            ctrls.currentItem = item;
            ctrls.visibleCb.Checked = item.VisiblityControlChecked;
            ctrls.pinnedBtn.Checked = item.PinControlChecked;
            ctrls.pinnedBtn.Image = item.PinControlChecked ? pinnedImage : pinImage;
            ctrls.textLabel.Text = item.Text;
            ctrls.textLabel.Progress = item.ProgressVisible ? item.ProgressValue : new double?();
            ctrls.textLabel.ForeColor = item.IsWarningText ? Color.Red : Color.Black;
        }

        void UpdateToolStripTextSizes()
        {
            foreach (var ctrls in
                dropDownPanel.Controls.OfType<ExtendedToolStrip>().Select(ts => (ToolStripControls)ts.Tag)
                .Union(Enumerable.Repeat(mainToolStripControls, 1))
                .Where(c => c != null))
            {
                int maxTextWidth = Math.Max(0, ctrls.toolStrip.ClientSize.Width - ctrls.pinnedBtn.Bounds.Right - ExtendedToolStrip.ResizeRectangleWidth - 5);
                ctrls.textLabel.Width = Math.Min(maxTextWidth, ctrls.textLabel.IntrinsicWidth);
            }
        }

        ToolStripControls AddToolStripControls(ExtendedToolStrip toolstrip)
        {
            var ctrls = new ToolStripControls()
            {
                toolStrip = toolstrip
            };
            ctrls.dropdownBtn = new ToolStripButton()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
            };
            ctrls.visibleCb = new CheckBox()
            {
                Padding = new Padding(0, 3, 0, 0),
            };
            ctrls.visibleCbHost = new ToolStripControlHost(ctrls.visibleCb)
            {
                Padding = new Padding(3, 0, 3, 0) // todo: check if scaling by DPI is needed
            };
            ctrls.pinnedBtn = new ToolStripButton()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
            };
            ctrls.textLabel = new ProgressToolStripLabel()
            {
                Padding = new Padding(6, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
            };
            toolstrip.Items.AddRange(new ToolStripItem[]
            {
                ctrls.dropdownBtn,
                ctrls.visibleCbHost,
                ctrls.pinnedBtn,
                ctrls.textLabel
            });
            toolstrip.ContextMenuStrip = new ContextMenuStrip();
            toolstrip.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                ctrls.visibleMenuItem = new ToolStripMenuItem("Visible", null, (s, e) => viewModel.OnMenuItemClicked(ctrls.currentItem, MenuItemId.Visible)),
                ctrls.pinnedMenuItem = new ToolStripMenuItem ("Pinned", null, (s, e) => viewModel.OnMenuItemClicked(ctrls.currentItem, MenuItemId.Pinned)),
                ctrls.visibleOnTimelineMenuItem = new ToolStripMenuItem("Display on timeline", null, (s, e) => viewModel.OnMenuItemClicked(ctrls.currentItem, MenuItemId.VisibleOnTimeline)),
                ctrls.deleteSeparatorMenuItem = new ToolStripMenuItem("-"),
                ctrls.deleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => viewModel.OnMenuItemClicked(ctrls.currentItem, MenuItemId.Delete)),
            });
            return ctrls;
        }

        private void closeSearchResultButton_Click(object sender, EventArgs e)
        {
            viewModel.OnCloseSearchResultsButtonClicked();
        }

        private void toggleBookmarkButton_Click(object sender, EventArgs e)
        {
            viewModel.OnToggleBookmarkButtonClicked();
        }

        private void findCurrentTimeButton_Click(object sender, EventArgs e)
        {
            viewModel.OnFindCurrentTimeButtonClicked();
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            viewModel.OnRefreshButtonClicked();
        }

        private void dropDownPanel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                viewModel.OnDropdownEscape();
            }
        }

        private void dropDownPanel_Leave(object sender, EventArgs e)
        {
            viewModel.OnDropdownContainerLostFocus();
        }

        class ToolStripControls
        {
            public ExtendedToolStrip toolStrip;
            public ToolStripButton dropdownBtn;
            public bool? dropdownBtnShowsList;
            public CheckBox visibleCb;
            public ToolStripControlHost visibleCbHost;
            public ToolStripButton pinnedBtn;
            public ProgressToolStripLabel textLabel;
            public ToolStripMenuItem visibleMenuItem, pinnedMenuItem,
                deleteMenuItem, deleteSeparatorMenuItem, visibleOnTimelineMenuItem;
            public bool isInitialized;
            public ViewItem currentItem;
        };

        class ProgressToolStripLabel : ToolStripLabel
        {
            double? progress;
            int intrinsicWidth;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public double? Progress
            {
                get
                {
                    return progress;
                }
                set
                {
                    progress = value;
                    Invalidate();
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public new string Text
            {
                get
                {
                    return base.Text;
                }
                set
                {
                    if (base.Text == value)
                        return;
                    base.Text = value;
                    intrinsicWidth = TextRenderer.MeasureText(value, Font).Width;
                }
            }

            public int IntrinsicWidth { get { return intrinsicWidth; } }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (progress != null)
                {
                    var b = new Rectangle(new Point(), Size);
                    b.Inflate(0, -b.Height / 6);
                    b.Width -= 1;
                    Color cl = Color.FromArgb(70, 0, 76, 255);
                    using (var pen = new Pen(cl))
                        e.Graphics.DrawRectangle(pen, b);
                    b.Width = (int)(b.Width * progress.Value);
                    using (var brush = new SolidBrush(cl))
                        e.Graphics.FillRectangle(brush, b);
                }
                base.OnPaint(e);
            }
        };

        private void dropDownPanel_Layout(object sender, LayoutEventArgs e)
        {
            UpdateToolStripTextSizes();
        }
    }
}
