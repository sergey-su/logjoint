using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
    public partial class SequenceDiagramVisualizerControl : UserControl, IView
    {
        IViewModel viewModel;
        Resources resources;
        DrawingUtils drawingUtils;
        ReadonlyRef<Size> arrowsAreaSize = new ReadonlyRef<Size>();

        public SequenceDiagramVisualizerControl()
        {
            InitializeComponent();

            var toolboxIconsSize = UIUtils.Dpi.Scale(14, 120);
            findCurrentTimeButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.SelectCurrentTime, toolboxIconsSize);
            nextBookmarkButton.Image = prevBookmarkButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.BigBookmark, toolboxIconsSize);
            prevUserActionButton.Image = nextUserActionButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.UserAction, toolboxIconsSize);
            zoomInButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.ZoomIn, toolboxIconsSize);
            zoomOutButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.ZoomOut, toolboxIconsSize);
            notificationsButton.Image = UIUtils.DownscaleUIImage(SequenceDiagramVisualizerControlResources.Warning, toolboxIconsSize);

            leftPanel.Width = UIUtils.Dpi.Scale(130);
            rolesCaptionsPanel.Height = tagsListContainerPanel.Height = UIUtils.Dpi.Scale(50);
        }

        void IView.SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            this.resources = new Resources(viewModel, Font.Name, Font.Size, UIUtils.Dpi.ScaleUp(1, 120))
            {
                FocusedMsgSlaveVert = new LJD.Image(SequenceDiagramVisualizerControlResources.FocusedMsgSlaveVert),
                FocusedMessageImage = new LJD.Image(SequenceDiagramVisualizerControlResources.FocusedMsgSlave),
                BookmarkImage = new LJD.Image(SequenceDiagramVisualizerControlResources.SmallBookmark),
                UserActionImage = new LJD.Image(SequenceDiagramVisualizerControlResources.UserAction)
            };

            InitializeArrowEndShapePoints();


            this.drawingUtils = new DrawingUtils(viewModel, resources);
            this.ParentForm.VisibleChanged += (s, e) =>
            {
                if (this.ParentForm.Visible) viewModel.OnWindowShown();
                else viewModel.OnWindowHidden();
            };

            var notificationsIconUpdater = Updaters.Create(() => viewModel.IsNotificationsIconVisibile,
                value => notificationsButton.Visible = value);

            var updateCurrentArrowControls = Updaters.Create(() => viewModel.CurrentArrowInfo,
                value =>
                {
                    currentArrowCaptionLabel.Text = value.Caption;
                    currentArrowDescription.Text = value.DescriptionText;
                    currentArrowDescription.Links.Clear();
                    foreach (var l in value.DescriptionLinks)
                        currentArrowDescription.Links.Add(new LinkLabel.Link()
                        {
                            LinkData = l.Item1,
                            Start = l.Item2,
                            Length = l.Item3
                        });
                }
            );

            var collapseResponsesCheckedUpdater = Updaters.Create(() => viewModel.IsCollapseResponsesChecked,
                value => collapseResponsesCheckbox.Checked = value);

            var collapseRoleInstancesChecked = Updaters.Create(() => viewModel.IsCollapseRoleInstancesChecked,
                value => collapseRoleInstancesCheckbox.Checked = value);

            var updateScrollBars = Updaters.Create(() => viewModel.ScrollInfo,
                value => UpdateScrollBars(value.vMax, value.vChange, value.vValue, value.hMax, value.hChange, value.hValue));

            var invalidateViews = Updaters.Create(() => viewModel.ArrowsDrawInfo, () => viewModel.RolesDrawInfo, (_1, _2) =>
            {
                rolesCaptionsPanel.Invalidate();
                arrowsPanel.Invalidate();
                leftPanel.Invalidate();
            });

            viewModel.ChangeNotification.CreateSubscription(() =>
            {
                notificationsIconUpdater();
                updateCurrentArrowControls();
                collapseResponsesCheckedUpdater();
                collapseRoleInstancesChecked();
                updateScrollBars();
                invalidateViews();
            });

            toastNotificationsListControl.SetViewModel(viewModel.ToastNotification);
        }

        ViewMetrics IView.GetMetrics()
        {
            return new ViewMetrics()
            {
                MessageHeight = UIUtils.Dpi.Scale(23, 120),
                NodeWidth = UIUtils.Dpi.Scale(200, 120),
                ExecutionOccurrenceWidth = UIUtils.Dpi.Scale(8, 120),
                ExecutionOccurrenceLevelOffset = UIUtils.Dpi.Scale(6, 120),
                ParallelNonHorizontalArrowsOffset = UIUtils.Dpi.Scale(4, 120),
                VScrollOffset = UIUtils.Dpi.Scale(50, 120),
            };
        }

        void IView.Show() => FindForm()?.Show();

        ReadonlyRef<Size> IView.ArrowsAreaSize => arrowsAreaSize;

        int IView.RolesCaptionsAreaHeight { get { return rolesCaptionsPanel.Height; } }

        void UpdateScrollBars(
            int vMax, int vChange, int vValue,
            int hMax, int hChange, int hValue
        )
        {
            void set(ScrollBar scroll, int max, int change, int value)
            {
                scroll.Enabled = change < max;
                scroll.Maximum = max;
                scroll.LargeChange = change;
                value = Math.Max(0, value);
                scroll.Value = value;
            }
            set(hScrollBar, hMax, hChange, hValue);
            set(vScrollBar, vMax, vChange, vValue);
        }

        Presenters.TagsList.IView IView.TagsListView
        {
            get { return tagsListControl; }
        }

        LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox
        {
            get { return quickSearchEditBox.InnerTextBox; }
        }

        void IView.PutInputFocusToArrowsArea()
        {
            if (arrowsPanel.CanFocus)
                arrowsPanel.Focus();
        }

        private void InitializeArrowEndShapePoints()
        {
            var scale = UIUtils.Dpi.Scale(1f, 120);
            this.resources.ArrowEndShapePoints = new[] {
                new LJD.PointF (-7 * scale, -4 * scale),
                new LJD.PointF (-7 * scale, +4 * scale),
                new LJD.PointF (0  * scale,  0 * scale)
            };
        }

        private void rolesCaptionsPanel_Paint(object sender, PaintEventArgs e)
        {
            if (drawingUtils == null)
                return;
            using (var g = new LJD.Graphics(e.Graphics))
                drawingUtils.DrawRoleCaptions(g);
        }

        private void rolesCaptionsPanel_SetCursor(object sender, HandledMouseEventArgs e)
        {
            if (drawingUtils == null)
                return;
            using (var g = new LJD.Graphics(CreateGraphics(), ownsGraphics: true))
            {
                var cursor = drawingUtils.GetRoleCaptionsCursor(g, e.Location.ToPoint());
                if (cursor == CursorKind.Hand)
                {
                    Cursor.Current = Cursors.Hand;
                    e.Handled = true;
                }
            }
        }

        private void rolesCaptionsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            using (var g = new LJD.Graphics(CreateGraphics(), ownsGraphics: true))
                drawingUtils.HandleRoleCaptionsMouseDown(g, e.Location.ToPoint());
        }

        private void arrowsPanel_Paint(object sender, PaintEventArgs e)
        {
            if (drawingUtils == null)
                return;

            using (var g = new LJD.Graphics(e.Graphics))
            {
                drawingUtils.DrawArrowsView(
                    g,
                    arrowsPanel.ClientSize.ToSize(),
                    r => ControlPaint.DrawFocusRectangle(e.Graphics, r.ToSystemDrawingObject())
                );
            }
        }

        private void arrowsPanel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;
            bool shift = (Control.ModifierKeys & Keys.Shift) != 0;
            if (e.KeyCode == Keys.Left)
            {
                e.IsInputKey = true;
                viewModel.OnKeyDown(Key.Left);
            }
            else if (e.KeyCode == Keys.Right)
            {
                e.IsInputKey = true;
                viewModel.OnKeyDown(Key.Right);
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.IsInputKey = true;
                if (ctrl)
                    viewModel.OnKeyDown(Key.ScrollLineUp);
                else
                    viewModel.OnKeyDown(Key.MoveSelectionUp);
            }
            else if (e.KeyCode == Keys.Down)
            {
                e.IsInputKey = true;
                if (ctrl)
                    viewModel.OnKeyDown(Key.ScrollLineDown);
                else
                    viewModel.OnKeyDown(Key.MoveSelectionDown);
            }
            else if (e.KeyValue == 187)
                viewModel.OnKeyDown(Key.Plus);
            else if (e.KeyValue == 189)
                viewModel.OnKeyDown(Key.Minus);
            else if (e.KeyCode == Keys.PageDown)
                viewModel.OnKeyDown(Key.PageDown);
            else if (e.KeyCode == Keys.PageUp)
                viewModel.OnKeyDown(Key.PageUp);
            else if (e.KeyCode == Keys.End)
                viewModel.OnKeyDown(Key.End);
            else if (e.KeyCode == Keys.Home)
                viewModel.OnKeyDown(Key.Home);
            else if (e.KeyCode == Keys.Enter)
                viewModel.OnKeyDown(Key.Enter);
            else if (e.KeyCode == Keys.F && e.Control)
                viewModel.OnKeyDown(Key.Find);
            else if (e.KeyCode == Keys.B)
                viewModel.OnKeyDown(Key.Bookmark);
            else if (e.KeyCode == Keys.F6)
                viewModel.OnKeyDown(Key.FindCurrentTimeShortcut);
            else if (e.KeyCode == Keys.F2 && !shift)
                viewModel.OnKeyDown(Key.NextBookmarkShortcut);
            else if (e.KeyCode == Keys.F2 && shift)
                viewModel.OnKeyDown(Key.PrevNextBookmarkShortcut);
        }

        static Key GetModifiers()
        {
            Key mod = 0;
            var mk = Control.ModifierKeys;
            if ((mk & Keys.Control) != 0)
                mod |= (Key.MultipleSelectionModifier | Key.WheelZoomModifier);
            return mod;
        }

        private void arrowsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var ctrl = (Control)sender;
            ctrl.Focus();
            viewModel.OnArrowsAreaMouseDown(e.Location.ToPoint(), e.Clicks >= 2);
        }

        private void arrowsPanel_MouseUp(object sender, MouseEventArgs e)
        {
            viewModel.OnArrowsAreaMouseUp(e.Location.ToPoint(), GetModifiers());
        }

        private void arrowsPanel_MouseMove(object sender, MouseEventArgs e)
        {
            viewModel.OnArrowsAreaMouseMove(e.Location.ToPoint());
        }

        private void arrowsPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            viewModel.OnArrowsAreaMouseWheel(e.Location.ToPoint(), e.Delta, GetModifiers());
        }

        private void currentArrowDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (e.Link.LinkData != null)
            {
                viewModel.OnTriggerClicked(e.Link.LinkData);
            }
        }

        private void leftPanel_Paint(object sender, PaintEventArgs e)
        {
            if (drawingUtils == null)
                return;
            using (var g = new LJD.Graphics(e.Graphics))
                drawingUtils.DrawLeftPanelView(g, leftPanel.PointToClient(arrowsPanel.PointToScreen(new System.Drawing.Point())).ToPoint(), leftPanel.ClientSize.ToSize());
        }

        private void arrowsPanel_Resize(object sender, EventArgs e)
        {
            arrowsAreaSize = new ReadonlyRef<Size>(arrowsPanel.ClientSize.ToSize());
            if (viewModel != null)
                viewModel.ChangeNotification.Post();
        }

        private void toolPanelLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            HandleMouseClick(sender);
        }

        void HandleMouseClick(object sender)
        {
            if (sender == prevUserActionButton)
                viewModel.OnPrevUserEventButtonClicked();
            else if (sender == nextUserActionButton)
                viewModel.OnNextUserEventButtonClicked();
            else if (sender == prevBookmarkButton)
                viewModel.OnPrevBookmarkButtonClicked();
            else if (sender == nextBookmarkButton)
                viewModel.OnNextBookmarkButtonClicked();
            else if (sender == findCurrentTimeButton)
                viewModel.OnFindCurrentTimeButtonClicked();
            else if (sender == zoomInButton)
                viewModel.OnZoomInButtonClicked();
            else if (sender == zoomOutButton)
                viewModel.OnZoomOutButtonClicked();
        }

        private void toolPanelLinkMouseDoubleClick(object sender, MouseEventArgs e)
        {
            HandleMouseClick(sender);
        }

        private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (viewModel != null)
                if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                    viewModel.OnScrolled(null, e.NewValue);
                else
                    viewModel.OnScrolled(e.NewValue, null);
        }

        private void leftPanel_MouseDown(object sender, MouseEventArgs e)
        {
            viewModel.OnLeftPanelMouseDown(arrowsPanel.PointToClient(leftPanel.PointToScreen(e.Location)).ToPoint(), e.Clicks >= 2, GetModifiers());
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (viewModel.OnEscapeCmdKey())
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void collapseResponsesCheckbox_Click(object sender, System.EventArgs e)
        {
            viewModel.OnCollapseResponsesChange(collapseResponsesCheckbox.Checked);
        }

        void collapseRoleInstancesCheckbox_Click(object sender, System.EventArgs e)
        {
            viewModel.OnCollapseRoleInstancesChange(collapseRoleInstancesCheckbox.Checked);
        }
        void notificationsButton_Click(object sender, System.EventArgs e)
        {
            viewModel.OnActiveNotificationButtonClicked();
        }
    }
}
