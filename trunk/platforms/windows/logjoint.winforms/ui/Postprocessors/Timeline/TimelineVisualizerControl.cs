using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public partial class TimelineVisualizerControl : UserControl, IView
	{
		IViewModel viewModel;
		readonly Font activitesCaptionsFont;
		readonly UIUtils.ToolTipHelper activitiesPanelToolTipHelper;
		readonly GraphicsResources res;
		readonly ControlDrawing drawing;
		CaptionsMarginMetrics captionsMarginMetrics;
		IChangeNotification changeNotification;
		ISubscription changeNotificationSubscription;
		Ref<Size> activitiesViewPanelSize;

		public TimelineVisualizerControl()
		{
			InitializeComponent();

			var toolboxIconsSize = UIUtils.Dpi.Scale(14, 120);
			prevUserActionButton.Image = nextUserActionButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.UserAction, toolboxIconsSize);
			prevBookmarkButton.Image = nextBookmarkButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.BigBookmark, toolboxIconsSize);
			findCurrentTimeButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.SelectCurrentTime, toolboxIconsSize);
			zoomInButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.ZoomIn, toolboxIconsSize);
			zoomOutButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.ZoomOut, toolboxIconsSize);
			notificationsButton.Image = UIUtils.DownscaleUIImage(TimelineVisualizerControlResources.Warning, toolboxIconsSize);

			res = new GraphicsResources(
				Font.FontFamily.Name,
				Font.Size,
				8f,
				6f,
				new LJD.Image(TimelineVisualizerControlResources.UserAction),
				new LJD.Image(TimelineVisualizerControlResources.APICall),
				new LJD.Image(TimelineVisualizerControlResources.TimelineBookmark),
				new LJD.Image(TimelineVisualizerControlResources.FocusedMsgSlaveVert),
				UIUtils.Dpi.ScaleUp(1, 120),
				new LJD.Brush(SystemColors.Control),
				1f
			);
			drawing = new ControlDrawing(res);

			activitesCaptionsFont = Font;

			var vm = GetUpToDateViewMetrics();

			var rulersPanelHeight = vm.RulersPanelHeight;

			activitiesContainer.SplitterDistance = UIUtils.Dpi.Scale(260, 120);
			activitiesContainer.SplitterWidth = UIUtils.Dpi.ScaleUp(3, 120);

			activitiesScrollBar.SmallChange = vm.LineHeight;
			activitiesScrollBar.Top = rulersPanelHeight;
			activitiesScrollBar.Height = activitiesScrollBar.Parent.Height - activitiesScrollBar.Top;

			activitiesViewPanel.MouseWheel += activitiesViewPanel_MouseWheel;
			activitesCaptionsPanel.MouseWheel += activitesCaptionsPanel_MouseWheel;

			quickSearchEditBox.Top = rulersPanelHeight - quickSearchEditBox.Height;
			panel5.Height = rulersPanelHeight;
			this.quickSearchEditBox.InnerTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.quickSearchEditBox_KeyDown);

			currentActivityCaptionLabel.Font = new Font(currentActivityCaptionLabel.Font, FontStyle.Bold);

			activitiesPanelToolTipHelper = new UIUtils.ToolTipHelper(activitiesViewPanel, GetActivitiesToolTipInfo, 150);

			// link labels do not scale on DPI properly. scale them manually.
			currentActivityDescription.Height = UIUtils.Dpi.ScaleUp(18, 120);
			currentActivitySourceLinkLabel.Height = UIUtils.Dpi.ScaleUp(18, 120);

			activitiesViewPanelSize = new Ref<Size>(activitiesViewPanel.Size);
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			this.changeNotification = viewModel.ChangeNotification;
			var updateNotificationsButton = Updaters.Create(() => viewModel.NotificationsIconVisibile, v => notificationsButton.Visible = v);
			var updateNoContentMessage = Updaters.Create(() => viewModel.NoContentMessageVisibile, SetNoContentMessageVisibility);
			var updateVertScroller = Updaters.Create(
				Selectors.Create(() => viewModel.ActivitiesCount, () => activitiesViewPanelSize, (activitiesCount, sz) => new { activitiesCount, sz }),
				key => UpdateActivitiesScroller(key.activitiesCount, key.sz.Value));
			var updateCurrentActivityInfo = Updaters.Create(() => viewModel.CurrentActivity, UpdateCurrentActivityControls);
			this.changeNotificationSubscription = changeNotification.CreateSubscription(() => {
				updateNotificationsButton();
				updateNoContentMessage();
				updateVertScroller();
				updateCurrentActivityInfo();
			});
			this.ParentForm.VisibleChanged += (s, e) =>
			{
				if (this.ParentForm.Visible) viewModel.OnWindowShown();
				else viewModel.OnWindowHidden();
			};
		}

		void IView.Invalidate(ViewAreaFlag flags)
		{
			if ((flags & ViewAreaFlag.NavigationPanelView) != 0)
				navigationPanel.Invalidate();
			if ((flags & ViewAreaFlag.ActivitiesCaptionsView) != 0)
				activitesCaptionsPanel.Invalidate();
			if ((flags & ViewAreaFlag.ActivitiesBarsView) != 0)
				activitiesViewPanel.Invalidate();
		}

		void IView.Refresh(ViewAreaFlag flags)
		{
			if ((flags & ViewAreaFlag.NavigationPanelView) != 0)
				navigationPanel.Refresh();
			if ((flags & ViewAreaFlag.ActivitiesCaptionsView) != 0)
				activitesCaptionsPanel.Refresh();
			if ((flags & ViewAreaFlag.ActivitiesBarsView) != 0)
				activitiesViewPanel.Refresh();
		}

		void IView.UpdateSequenceDiagramAreaMetrics()
		{
			using (var g = new LJD.Graphics(CreateGraphics(), ownsGraphics: true))
			{
				captionsMarginMetrics = GetUpToDateViewMetrics().ComputeCaptionsMarginMetrics(g, viewModel);
			}
		}

		void UpdateCurrentActivityControls(CurrentActivityDrawInfo data)
		{
			currentActivityCaptionLabel.Text = data.Caption;
			currentActivityDescription.Text = data.DescriptionText;
			currentActivityDescription.Links.Clear();
			if (data.DescriptionLinks != null)
			{
				foreach (var l in data.DescriptionLinks)
					currentActivityDescription.Links.Add(new LinkLabel.Link()
					{
						LinkData = l.Item1,
						Start = l.Item2,
						Length = l.Item3
					});
			}
			currentActivitySourceLinkLabel.Visible = data.SourceText != null;
			if (data.SourceText != null)
			{
				currentActivitySourceLinkLabel.Text = data.SourceText;
				currentActivitySourceLinkLabel.Links.Clear();
				currentActivitySourceLinkLabel.Links.Add(new LinkLabel.Link()
					{
						LinkData = data.SourceLink.Item1,
						Start = data.SourceLink.Item2,
						Length = data.SourceLink.Item3
					});
			}
		}

		HitTestResult IView.HitTest(object hitTestToken)
		{
			var htToken = hitTestToken as HitTestToken;
			if (htToken == null)
				return new HitTestResult();
			return GetUpToDateViewMetrics().HitTest(htToken.Pt, viewModel, 
				htToken.Control == activitesCaptionsPanel ? HitTestResult.AreaCode.CaptionsPanel :
				htToken.Control == navigationPanel ? HitTestResult.AreaCode.NavigationPanel :
				HitTestResult.AreaCode.ActivitiesPanel,
				() => new LJD.Graphics(CreateGraphics(), ownsGraphics: true));
		}

		void IView.EnsureActivityVisible(int activityIndex)
		{
			var vm = GetUpToDateViewMetrics();
			int y = vm.GetActivityY(activityIndex);
			if (y > 0 && (y + vm.LineHeight) < activitiesViewPanel.Height)
				return;
			var scrollerPos = vm.RulersPanelHeight - activitiesViewPanel.Height / 2 + activityIndex * vm.LineHeight;
			scrollerPos = Math.Max(0, scrollerPos);
			activitiesScrollBar.Value = scrollerPos;
		}

		void IView.ReceiveInputFocus()
		{
			if (activitiesViewPanel.CanFocus)
				activitiesViewPanel.Focus();
		}

		LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox
		{
			get { return quickSearchEditBox.InnerTextBox; }
		}

		LogJoint.UI.Presenters.TagsList.IView IView.TagsListView
		{
			get { return tagsListControl; }
		}

		Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView
		{
			get { return toastNotificationsListControl; }
		}

		void SetNoContentMessageVisibility(bool value)
		{
			if (noContentLink.Text.Length == 0)
			{
				noContentLink.Text = "Nothing visible.\r\nSearch <<left. Search right>>";
				noContentLink.Links.Add(new LinkLabel.Link(25, 6, "l"));
				noContentLink.Links.Add(new LinkLabel.Link(40, 7, "r"));
			};
			noContentLink.Visible = value;
		}

		private void UpdateActivitiesScroller(int activitiesCount, Size activitiesViewSize)
		{
			var vm = GetUpToDateViewMetrics();
			activitiesScrollBar.Maximum = activitiesCount * vm.LineHeight;
			activitiesScrollBar.LargeChange = Math.Max(1, activitiesViewSize.Height - vm.RulersPanelHeight);
			activitiesScrollBar.Enabled = activitiesScrollBar.Maximum > activitiesScrollBar.LargeChange;
			if (activitiesScrollBar.Enabled)
			{
				var maxValue = activitiesScrollBar.Maximum - activitiesScrollBar.LargeChange;
				if (activitiesScrollBar.Value > maxValue)
					activitiesScrollBar.Value = maxValue;
			}
			else
			{
				activitiesScrollBar.Value = 0;
			}
		}

		private void activitesCaptionsPanel_Paint(object sender, PaintEventArgs e)
		{
			if (viewModel == null)
				return;
			using (var g = new LJD.Graphics(e.Graphics))
			{
				drawing.DrawCaptionsView(
					g,
					GetUpToDateViewMetrics(),
					viewModel,
					(text, textRect, hlbegin, hllen, isFailure) =>
					{
						if (hllen > 0 && hlbegin >= 0)
						{
							var bogusSz = new Size(10000, 10000);
							var highlightLeft = textRect.X + TextRenderer.MeasureText(
								e.Graphics,
								text.Substring(0, hlbegin),
								activitesCaptionsFont,
								bogusSz,
								TextFormatFlags.NoPadding).Width;
							var highlightWidth = TextRenderer.MeasureText(
								e.Graphics,
								text.Substring(hlbegin, hllen),
								activitesCaptionsFont,
								bogusSz,
								TextFormatFlags.NoPadding).Width;
							e.Graphics.FillRectangle(Brushes.Yellow, new RectangleF(highlightLeft, textRect.Y, highlightWidth, textRect.Height));
						}
						TextRenderer.DrawText(e.Graphics, text, activitesCaptionsFont, textRect,
							isFailure ? Color.Red : Color.Black,
							TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter |
							TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPadding);
					}
				);
			}
		}

		static Brush MakeBrush(ModelColor c)
		{
			return new SolidBrush(Color.FromArgb(c.R, c.G, c.B));
		}

		private void activitiesViewPanel_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.White, e.ClipRectangle);
			if (viewModel == null)
				return;
			using (var g = new LJD.Graphics(e.Graphics))
				drawing.DrawActivtiesView(g, GetUpToDateViewMetrics(), viewModel);
		}

		static KeyCode GetKeyModifiers()
		{
			var ret = KeyCode.None;
			var mk = Control.ModifierKeys;
			if ((mk & Keys.Shift) != 0)
				ret |= KeyCode.Shift;
			if ((mk & Keys.Control) != 0)
				ret |= KeyCode.Ctrl;
			return ret;
		}

		private void activitiesPanel_MouseDown(object sender, MouseEventArgs e)
		{
			if (viewModel == null)
				return;
			var ctrl = (Control)sender;
			ctrl.Focus();
			viewModel.OnMouseDown(new HitTestToken(ctrl, e), GetKeyModifiers(), e.Clicks == 2);
		}

		private void activitiesViewPanel_MouseMove(object sender, MouseEventArgs e)
		{
			viewModel?.OnMouseMove(new HitTestToken((Control)sender, e), GetKeyModifiers());
		}

		private void activitiesViewPanel_MouseUp(object sender, MouseEventArgs e)
		{
			viewModel?.OnMouseUp(new HitTestToken((Control)sender, e));
		}

		private void activitiesPanel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
				viewModel.OnKeyDown(KeyCode.Up);
			else if (e.KeyCode == Keys.Down)
				viewModel.OnKeyDown(KeyCode.Down);
			else if (e.KeyCode == Keys.Left)
				viewModel.OnKeyDown(KeyCode.Left);
			else if (e.KeyCode == Keys.Right)
				viewModel.OnKeyDown(KeyCode.Right);
			else if (e.KeyValue == 187)
				viewModel.OnKeyDown(KeyCode.Plus | GetKeyModifiers());
			else if (e.KeyValue == 189)
				viewModel.OnKeyDown(KeyCode.Minus | GetKeyModifiers());
			else if (e.KeyCode == Keys.Enter)
				viewModel.OnKeyDown(KeyCode.Enter);
			else if (e.KeyCode == Keys.F && e.Control)
				viewModel.OnKeyDown(KeyCode.Find);
			else if (e.KeyCode == Keys.F6)
				viewModel.OnKeyDown(KeyCode.FindCurrentTimeShortcut);
			else if (e.KeyCode == Keys.F2 && !e.Shift)
				viewModel.OnKeyDown(KeyCode.NextBookmarkShortcut);
			else if (e.KeyCode == Keys.F2 && e.Shift)
				viewModel.OnKeyDown(KeyCode.PrevBookmarkShortcut);
		}

		private void activitiesViewPanel_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (Control.ModifierKeys == Keys.None)
			{
				viewModel.OnKeyPressed(e.KeyChar);
				e.Handled = true;
			}
		}

		void activitiesViewPanel_MouseWheel(object sender, MouseEventArgs e)
		{
			if (activitiesViewPanel.Width > 0 && Control.ModifierKeys == Keys.Control)
			{
				viewModel.OnMouseZoom((double)e.X / (double)activitiesViewPanel.Width, e.Delta);
			}
			else if (Control.ModifierKeys == Keys.None)
			{
				HandleActivitiesScroll(e);
			}
		}

		void activitesCaptionsPanel_MouseWheel(object sender, MouseEventArgs e)
		{
			if (Control.ModifierKeys == Keys.None)
			{
				HandleActivitiesScroll(e);
			}
		}

		void HandleActivitiesScroll(MouseEventArgs e)
		{
			var newValue = activitiesScrollBar.Value - Math.Sign(e.Delta) * 4 * GetUpToDateViewMetrics().LineHeight;
			newValue = Math.Min(activitiesScrollBar.Maximum - activitiesScrollBar.LargeChange + 1, newValue);
			newValue = Math.Max(activitiesScrollBar.Minimum, newValue);
			activitiesScrollBar.Value = newValue;
			activitesCaptionsPanel.Invalidate();
			activitiesViewPanel.Invalidate();
		}

		private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
		{
			activitesCaptionsPanel.Refresh();
			activitiesViewPanel.Refresh();
		}

		private void activitiesViewPanel_Resize(object sender, EventArgs e)
		{
			activitiesViewPanelSize = new Ref<Size>(activitiesViewPanel.Size);
			changeNotification?.Post();
		}

		private void navigationPanel_Paint(object sender, PaintEventArgs e)
		{
			if (viewModel == null)
				return;
			using (var g = new LJD.Graphics(e.Graphics))
				drawing.DrawNavigationPanel(g, GetUpToDateViewMetrics(), viewModel);
		}

		private void navigationPanel_SetCursor(object sender, HandledMouseEventArgs e)
		{
			if (viewModel != null)
				HandleHandledMouseEventArgs(GetUpToDateViewMetrics().GetNavigationPanelCursor(e.Location, viewModel), e);
		}

		private void navigationPanel_MouseDown(object sender, MouseEventArgs e)
		{
			viewModel?.OnMouseDown(new HitTestToken((Control)sender, e), GetKeyModifiers(), e.Clicks == 2);
		}

		private void navigationPanel_MouseUp(object sender, MouseEventArgs e)
		{
			viewModel?.OnMouseUp(new HitTestToken((Control)sender, e));
		}

		private void navigationPanel_MouseMove(object sender, MouseEventArgs e)
		{
			viewModel?.OnMouseMove(new HitTestToken((Control)sender, e), GetKeyModifiers());
		}

		private void activitiesViewPanel_SetCursor(object sender, HandledMouseEventArgs e)
		{
			if (viewModel != null)
				HandleHandledMouseEventArgs(GetUpToDateViewMetrics().GetActivitiesPanelCursor(e.Location, viewModel, 
					() => new LJD.Graphics(CreateGraphics(), ownsGraphics: true)), e);
		}

		void HandleHandledMouseEventArgs(CursorType c, HandledMouseEventArgs e)
		{
			if (c == CursorType.Hand)
			{
				Cursor.Current = Cursors.Hand;
				e.Handled = true;
			}
			else if (c == CursorType.SizeAll)
			{
				Cursor.Current = Cursors.SizeAll;
				e.Handled = true;
			}
			else if (c == CursorType.SizeWE)
			{
				Cursor.Current = Cursors.SizeWE;
				e.Handled = true;
			}
		}

		private void activitiesContainer_DoubleClick(object sender, EventArgs e)
		{
			if (viewModel == null)
				return;
			var maxWidth = 0;
			using (var dc = CreateGraphics())
				foreach (var a in viewModel.OnDrawActivities())
					maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(dc, a.Caption, activitesCaptionsFont).Width);
			if (maxWidth != 0)
			{
				maxWidth = Math.Min(maxWidth, activitiesContainer.Width / 2); // protect from insanely big captions
				activitiesContainer.SplitterDistance = maxWidth;
			}
		}

		private void currentActivityDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (e.Link.LinkData != null)
			{
				viewModel.OnActivityTriggerClicked(e.Link.LinkData);
			}
		}

		private void currentActivitySourceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (e.Link.LinkData != null)
			{
				viewModel.OnActivitySourceLinkClicked(e.Link.LinkData);
			}
		}

		private void activitesCaptionsPanel_Resize(object sender, EventArgs e)
		{
			((IView)this).UpdateSequenceDiagramAreaMetrics();
		}

		LogJoint.UI.UIUtils.ToolTipInfo GetActivitiesToolTipInfo(Point pt)
		{
			if (viewModel == null)
				return null;
			var toolTip = viewModel.OnToolTip(new HitTestToken(activitiesViewPanel, pt)) ?? "";
			if (string.IsNullOrEmpty(toolTip))
				return null;
			var ret = new UIUtils.ToolTipInfo()
			{
				Text = toolTip,
				Title = null,
				Duration = 5000
			};
			return ret;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				if (viewModel.OnEscapeCmdKey())
					return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void quickSearchEditBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				e.Handled = true;
				viewModel.OnQuickSearchExitBoxKeyDown(KeyCode.Up);
			}
			else if (e.KeyCode == Keys.Down)
			{
				e.Handled = true;
				viewModel.OnQuickSearchExitBoxKeyDown(KeyCode.Down);
			}
		}

		private void toolPanelLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			HandleMouseClick(sender);
		}

		private void toolPanelLinkMouseDoubleClick(object sender, MouseEventArgs e)
		{
			HandleMouseClick(sender);
		}

		private void noContentLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			viewModel.OnNoContentLinkClicked(searchLeft: e.Link.LinkData as string == "l");
		}

		void notificationsButton_Click(object sender, System.EventArgs e)
		{
			viewModel.OnActiveNotificationButtonClicked();
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

		ViewMetrics GetUpToDateViewMetrics()
		{
			var viewMetrics = new ViewMetrics(res);
			var vm = viewMetrics;
			vm.ActivitiesViewWidth = activitiesViewPanel.Width;
			vm.ActivitiesViewHeight = activitiesViewPanel.Height;
			vm.ActivitesCaptionsViewWidth = activitesCaptionsPanel.Width;
			vm.ActivitesCaptionsViewHeight = activitesCaptionsPanel.Height;
			vm.NavigationPanelWidth = navigationPanel.Width;
			vm.NavigationPanelHeight = navigationPanel.Height;
			vm.VScrollBarValue = activitiesScrollBar.Value;

			vm.LineHeight = UIUtils.Dpi.Scale(20, 120);
			vm.DPIScale = UIUtils.Dpi.Scale(1f);
			vm.ActivityBarRectPaddingY = UIUtils.Dpi.Scale(5, 120);
			vm.TriggerLinkWidth = UIUtils.Dpi.ScaleUp(5, 120);
			vm.DistanceBetweenRulerMarks = UIUtils.Dpi.ScaleUp(40, 120); ;
			vm.MeasurerTop = 25;
			vm.VisibleRangeResizerWidth = 8;
			vm.RulersPanelHeight = UIUtils.Dpi.Scale(53, 120);
			vm.ActionLebelHeight = UIUtils.Dpi.Scale(20, 120);

			vm.SequenceDiagramAreaWidth = captionsMarginMetrics.SequenceDiagramAreaWidth;
			vm.FoldingAreaWidth = captionsMarginMetrics.FoldingAreaWidth;

			return viewMetrics;
		}

		class HitTestToken
		{
			public readonly Control Control;
			public readonly Point Pt;
			public HitTestToken(Control ctrl, Point pt)
			{
				Control = ctrl;
				Pt = pt;
			}
			public HitTestToken(Control ctrl, MouseEventArgs e) : this(ctrl, e.Location) { }
		};
	}
}
