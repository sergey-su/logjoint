using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	public partial class SequenceDiagramVisualizerControl : UserControl, IView
	{
		IViewEvents eventsHandler;
		Resources resources;
		DrawingUtils drawingUtils;

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

			this.resources = new Resources(Font.Name, Font.Size, UIUtils.Dpi.ScaleUp(1, 120));
			this.resources.FocusedMsgSlaveVert = new LJD.Image(SequenceDiagramVisualizerControlResources.FocusedMsgSlaveVert);
			this.resources.FocusedMessageImage = new LJD.Image(SequenceDiagramVisualizerControlResources.FocusedMsgSlave);
			this.resources.BookmarkImage = new LJD.Image(SequenceDiagramVisualizerControlResources.SmallBookmark);
			this.resources.UserActionImage = new LJD.Image(SequenceDiagramVisualizerControlResources.UserAction);

			InitializeArrowEndShapePoints();

			leftPanel.Width = UIUtils.Dpi.Scale(130);
			rolesCaptionsPanel.Height = tagsListContainerPanel.Height = UIUtils.Dpi.Scale(50);
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			this.drawingUtils = new DrawingUtils(eventsHandler, resources);
			this.ParentForm.VisibleChanged += (s, e) =>
			{
				if (this.ParentForm.Visible) eventsHandler.OnWindowShown();
				else eventsHandler.OnWindowHidden();
			};
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

		void IView.Invalidate()
		{
			rolesCaptionsPanel.Invalidate();
			arrowsPanel.Invalidate();
			leftPanel.Invalidate();
		}

		int IView.ArrowsAreaWidth { get { return arrowsPanel.Width; } }

		int IView.ArrowsAreaHeight { get { return arrowsPanel.Height; } }

		int IView.RolesCaptionsAreaHeight { get { return rolesCaptionsPanel.Height; } }

		void IView.UpdateCurrentArrowControls(
			string caption,
			string descriptionText, IEnumerable<Tuple<object, int, int>> descriptionLinks)
		{
			currentArrowCaptionLabel.Text = caption;
			currentArrowDescription.Text = descriptionText;
			currentArrowDescription.Links.Clear();
			if (descriptionLinks != null)
			{
				foreach (var l in descriptionLinks)
					currentArrowDescription.Links.Add(new LinkLabel.Link()
					{
						LinkData = l.Item1,
						Start = l.Item2,
						Length = l.Item3
					});
			}
		}

		void IView.UpdateScrollBars(
			int vMax, int vChange, int vValue,
			int hMax, int hChange, int hValue
		)
		{
			Action<ScrollBar, int, int, int> set = (scroll, max, change, value) =>
			{
				scroll.Enabled = change < max;
				scroll.Maximum = max;
				scroll.LargeChange = change;
				value = Math.Max(0, value);
				scroll.Value = value;
			};
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

		bool IView.IsCollapseResponsesChecked
		{
			get { return collapseResponsesCheckbox.Checked; }
			set { collapseResponsesCheckbox.Checked = value; }
		}

		bool IView.IsCollapseRoleInstancesChecked
		{
			get { return collapseRoleInstancesCheckbox.Checked; }
			set { collapseRoleInstancesCheckbox.Checked = value; }
		}

		Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView
		{
			get { return toastNotificationsListControl; }
		}

		void IView.SetNotificationsIconVisibility(bool value)
		{
			notificationsButton.Visible = value;
		}

		private void InitializeArrowEndShapePoints()
		{
			this.resources.ArrowEndShapePoints = new[] {
				new PointF (-7, -4),
				new PointF (-7, +4),
				new PointF (0, 0)
			};
			using (var mtx = new Matrix())
			{
				var scale = UIUtils.Dpi.Scale(1f, 120);
				mtx.Scale(scale, scale);
				mtx.TransformPoints(this.resources.ArrowEndShapePoints);
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
				var cursor = drawingUtils.GetRoleCaptionsCursor(g, e.Location);
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
				drawingUtils.HandleRoleCaptionsMouseDown(g, e.Location);
		}

		private void arrowsPanel_Paint(object sender, PaintEventArgs e)
		{
			if (drawingUtils == null)
				return;

			using (var g = new LJD.Graphics(e.Graphics))
			{
				drawingUtils.DrawArrowsView(
					g,
					arrowsPanel.ClientSize,
					r => ControlPaint.DrawFocusRectangle(e.Graphics, r)
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
				eventsHandler.OnKeyDown(Key.Left);
			}
			else if (e.KeyCode == Keys.Right)
			{
				e.IsInputKey = true;
				eventsHandler.OnKeyDown(Key.Right);
			}
			else if (e.KeyCode == Keys.Up)
			{
				e.IsInputKey = true;
				if (ctrl)
					eventsHandler.OnKeyDown(Key.ScrollLineUp);
				else
					eventsHandler.OnKeyDown(Key.MoveSelectionUp);
			}
			else if (e.KeyCode == Keys.Down)
			{
				e.IsInputKey = true;
				if (ctrl)
					eventsHandler.OnKeyDown(Key.ScrollLineDown);
				else
					eventsHandler.OnKeyDown(Key.MoveSelectionDown);
			}
			else if (e.KeyValue == 187)
				eventsHandler.OnKeyDown(Key.Plus);
			else if (e.KeyValue == 189)
				eventsHandler.OnKeyDown(Key.Minus);
			else if (e.KeyCode == Keys.PageDown)
				eventsHandler.OnKeyDown(Key.PageDown);
			else if (e.KeyCode == Keys.PageUp)
				eventsHandler.OnKeyDown(Key.PageUp);
			else if (e.KeyCode == Keys.End)
				eventsHandler.OnKeyDown(Key.End);
			else if (e.KeyCode == Keys.Home)
				eventsHandler.OnKeyDown(Key.Home);
			else if (e.KeyCode == Keys.Enter)
				eventsHandler.OnKeyDown(Key.Enter);
			else if (e.KeyCode == Keys.F && e.Control)
				eventsHandler.OnKeyDown(Key.Find);
			else if (e.KeyCode == Keys.B)
				eventsHandler.OnKeyDown(Key.Bookmark);
			else if (e.KeyCode == Keys.F6)
				eventsHandler.OnKeyDown(Key.FindCurrentTimeShortcut);
			else if (e.KeyCode == Keys.F2 && !shift)
				eventsHandler.OnKeyDown(Key.NextBookmarkShortcut);
			else if (e.KeyCode == Keys.F2 && shift)
				eventsHandler.OnKeyDown(Key.PrevNextBookmarkShortcut);
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
			eventsHandler.OnArrowsAreaMouseDown(e.Location, e.Clicks >= 2);
		}

		private void arrowsPanel_MouseUp(object sender, MouseEventArgs e)
		{
			eventsHandler.OnArrowsAreaMouseUp(e.Location, GetModifiers());
		}

		private void arrowsPanel_MouseMove(object sender, MouseEventArgs e)
		{
			eventsHandler.OnArrowsAreaMouseMove(e.Location);
		}

		private void arrowsPanel_MouseWheel(object sender, MouseEventArgs e)
		{
			eventsHandler.OnArrowsAreaMouseWheel(e.Location, e.Delta, GetModifiers());
		}

		private void currentArrowDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (e.Link.LinkData != null)
			{
				eventsHandler.OnTriggerClicked(e.Link.LinkData);
			}
		}

		private void leftPanel_Paint(object sender, PaintEventArgs e)
		{
			if (drawingUtils == null)
				return;
			using (var g = new LJD.Graphics(e.Graphics))
				drawingUtils.DrawLeftPanelView(g, leftPanel.PointToClient(arrowsPanel.PointToScreen(new Point())), leftPanel.ClientSize);
		}

		private void arrowsPanel_Resize(object sender, EventArgs e)
		{
			if (eventsHandler != null)
				eventsHandler.OnResized();
		}

		private void toolPanelLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			HandleMouseClick(sender);
		}

		void HandleMouseClick(object sender)
		{
			if (sender == prevUserActionButton)
				eventsHandler.OnPrevUserEventButtonClicked();
			else if (sender == nextUserActionButton)
				eventsHandler.OnNextUserEventButtonClicked();
			else if (sender == prevBookmarkButton)
				eventsHandler.OnPrevBookmarkButtonClicked();
			else if (sender == nextBookmarkButton)
				eventsHandler.OnNextBookmarkButtonClicked();
			else if (sender == findCurrentTimeButton)
				eventsHandler.OnFindCurrentTimeButtonClicked();
			else if (sender == zoomInButton)
				eventsHandler.OnZoomInButtonClicked();
			else if (sender == zoomOutButton)
				eventsHandler.OnZoomOutButtonClicked();
		}

		private void toolPanelLinkMouseDoubleClick(object sender, MouseEventArgs e)
		{
			HandleMouseClick(sender);
		}

		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			if (eventsHandler != null)
				if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
					eventsHandler.OnScrolled(null, e.NewValue);
				else
					eventsHandler.OnScrolled(e.NewValue, null);
		}

		private void leftPanel_MouseDown(object sender, MouseEventArgs e)
		{
			eventsHandler.OnLeftPanelMouseDown(arrowsPanel.PointToClient(leftPanel.PointToScreen(e.Location)), e.Clicks >= 2, GetModifiers());
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				if (eventsHandler.OnEscapeCmdKey())
					return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		void collapseResponsesCheckbox_Click(object sender, System.EventArgs e)
		{
			eventsHandler.OnCollapseResponsesChanged();
		}

		void collapseRoleInstancesCheckbox_Click(object sender, System.EventArgs e)
		{
			eventsHandler.OnCollapseRoleInstancesChanged();
		}
		void notificationsButton_Click(object sender, System.EventArgs e)
		{
			eventsHandler.OnActiveNotificationButtonClicked();
		}
	}
}
