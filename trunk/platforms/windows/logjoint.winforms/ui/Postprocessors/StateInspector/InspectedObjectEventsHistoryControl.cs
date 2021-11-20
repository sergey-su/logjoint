using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class InspectedObjectEventsHistoryControl : UserControl
	{
		IViewModel viewModel;
		Windows.Reactive.IListBoxController<IStateHistoryItem> listBoxController;
		StringFormat strFormat;
		float bookmarkAndFocusedMarkAreaWidth;
		BufferedGraphicsContext bufferedGraphicsContext = new BufferedGraphicsContext();

		public InspectedObjectEventsHistoryControl()
		{
			InitializeComponent();
			using (Graphics g = this.CreateGraphics())
				listBox.ItemHeight = (int)(15.0 * g.DpiY / 96.0);
			bookmarkAndFocusedMarkAreaWidth = UIUtils.Dpi.Scale(13);
		}

		public void Init(IViewModel viewModel, Windows.Reactive.IReactive reactive)
		{
			this.viewModel = viewModel;
			this.listBoxController = reactive.CreateListBoxController<IStateHistoryItem>(this.listBox);
			this.listBoxController.OnSelect = this.viewModel.OnChangeHistoryChangeSelection;
			this.listBoxController.OnUpdateRow = (item, index, oldItem) =>
			{
				if (oldItem != null)
					listBox.Invalidate(listBox.GetItemRectangle(index));
			};

			var updateStateHistoty = Updaters.Create(
				() => viewModel.ChangeHistoryItems,
				listBoxController.Update
			);

			var updateBookmarksAndFocusedMessageMark = Updaters.Create(
				() => viewModel.IsChangeHistoryItemBookmarked,
				() => viewModel.FocusedMessagePositionInChangeHistory,
				(_1, _2) => listBox.Invalidate(new Rectangle(0, 0, (int)bookmarkAndFocusedMarkAreaWidth + 1, listBox.Height))
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateStateHistoty();
				updateBookmarksAndFocusedMessageMark();
			});
		}

		public ExtendedToolStrip Header { get { return toolStrip1; } }

		IStateHistoryItem PrimarySelectedItem => listBoxController.Map(listBox.SelectedItem) as IStateHistoryItem;

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
				return cp;
			}
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			listBox.Capture = false;
			if (PrimarySelectedItem != null)
				viewModel.OnChangeHistoryItemDoubleClicked(PrimarySelectedItem);
		}

		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
		{
			// drawing uses double buffering to ensure good look over RDP session.
			// direct drawing to e.Graphics in RDP session disables font smoothing for some reason.
			using (var backBuffer = bufferedGraphicsContext.Allocate(e.Graphics, e.Bounds)) 
			{
				var g = backBuffer.Graphics;

				using (var bkBrush = new SolidBrush(e.BackColor))
				{
					g.FillRectangle(bkBrush, e.Bounds);
				}
				if (e.Index >= 0) // Index was observed to be -1 once in a while!
				{
					if (listBoxController.Map(listBox.Items[e.Index]) is IStateHistoryItem li)
					{
						var bmkImg = StateInspectorResources.Bookmark;
						var bmkImgSz = bmkImg.GetSize(width: UIUtils.Dpi.Scale(9));

						bool isBookmarked = viewModel.IsChangeHistoryItemBookmarked(li);
						if (isBookmarked)
						{
							var r = listBox.GetItemRectangle(e.Index);
							g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
							g.DrawImage(bmkImg, new RectangleF(1, r.Y + (r.Height - bmkImgSz.Height) / 2, bmkImgSz.Width, bmkImgSz.Height));
						}

						var focusedMessage = viewModel.FocusedMessagePositionInChangeHistory;
						if (focusedMessage != null && listBox.Items.Count != 0 && Math.Abs(focusedMessage.Item1 - e.Index) <= 1)
						{
							var imgsz = UIUtils.FocusedItemMarkBounds.Size;
							float y = focusedMessage.Item1 >= listBox.Items.Count ?
								listBox.GetItemRectangle(listBox.Items.Count - 1).Bottom :
								listBox.GetItemRectangle(focusedMessage.Item1).Top;
							if (focusedMessage.Item1 != focusedMessage.Item2)
								y += listBox.ItemHeight / 2;
							if (y == 0)
								y += imgsz.Height / 2;
							UIUtils.DrawFocusedItemMark(g, bmkImgSz.Width + 2, y, drawWhiteBounds: true);
						}

						using (var b = new SolidBrush(e.ForeColor))
						{
							if (strFormat == null)
							{
								strFormat = new StringFormat(StringFormatFlags.NoFontFallback);
								strFormat.SetTabStops(0, new float[] { bookmarkAndFocusedMarkAreaWidth, g.MeasureString("12:23:45.678", e.Font).Width + 5 });
							}
							g.DrawString($"\t{li.Time}\t{li.Message}", e.Font, b, e.Bounds.Location, strFormat);
						}
					}
				}

				backBuffer.Render(e.Graphics);
			}

			e.DrawFocusRectangle();
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (PrimarySelectedItem != null)
			{
				Key k = Key.None;
				if (keyData == Keys.Enter)
					k = Key.Enter;
				else if (keyData == Keys.B)
					k = Key.BookmarkShortcut;
				else if (keyData == (Keys.C | Keys.Control))
					k = Key.CopyShortcut;
				if (k != Key.None)
				{
					viewModel.OnChangeHistoryItemKeyEvent(PrimarySelectedItem, k);
					return false;
				}
			}
			return base.ProcessDialogKey(keyData);
		}
	}
}
