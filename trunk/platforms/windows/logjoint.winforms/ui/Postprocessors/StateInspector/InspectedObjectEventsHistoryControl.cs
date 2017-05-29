using System;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class InspectedObjectEventsHistoryControl : UserControl
	{
		IViewEvents viewEvents;
		bool isUpdating;
		int topIndexBeforeUpdate;
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

		public void Init(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		public ExtendedToolStrip Header { get { return toolStrip1; } }

		public IEnumerable<StateHistoryItem> GetSelection()
		{
			foreach (int i in listBox.SelectedIndices)
				yield return ((ListItem)listBox.Items[i]).evt;
		}

		StateHistoryItem PrimarySelectedItem
		{
			get
			{
				var i = listBox.SelectedItem as ListItem;
				if (i != null)
					return i.evt;
				return null;
			}
		}

		public void BeginUpdate(bool fullUpdate, bool clearList)
		{
			if (fullUpdate)
			{
				isUpdating = true;
				topIndexBeforeUpdate = listBox.TopIndex;
				listBox.BeginUpdate();
			}
			if (clearList)
			{
				listBox.Items.Clear();
			}
		}

		public int AddItem(StateHistoryItem item)
		{
			return listBox.Items.Add(new ListItem(item));
		}

		public void EndUpdate(int[] newSelectedIndexes, bool fullUpdate, bool redrawFocusedMessageMark)
		{
			if (newSelectedIndexes != null)
			{
				listBox.SelectedIndices.Clear();
				foreach (var i in newSelectedIndexes)
					listBox.SelectedIndices.Add(i);
			}
			if (fullUpdate)
			{
				isUpdating = false;
				listBox.EndUpdate();
				if (topIndexBeforeUpdate < listBox.Items.Count)
					listBox.TopIndex = topIndexBeforeUpdate;
			}
			if (redrawFocusedMessageMark)
			{
				listBox.Invalidate(new Rectangle(0, 0, (int)bookmarkAndFocusedMarkAreaWidth+1, listBox.Height));
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
				return cp;
			}
		}

		class ListItem
		{
			public ListItem(StateHistoryItem evt)
			{
				this.evt = evt;
			}

			public override string ToString()
			{
				return "\t" + evt.Time + "\t" + evt.Message;
			}

			public readonly StateHistoryItem evt;
		};

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			listBox.Capture = false;
			if (PrimarySelectedItem != null)
				viewEvents.OnChangeHistoryItemClicked(PrimarySelectedItem);
		}

		void listBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (!isUpdating)
				viewEvents.OnChangeHistorySelectionChanged();
		}

		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
		{
			// drawing uses double buffering to ensure good look over RDP session.
			// direct drawing to e.Graphics in DRP session disables font smoothing for some reason.
			using (var backBuffer = bufferedGraphicsContext.Allocate(e.Graphics, e.Bounds)) 
			{
				var g = backBuffer.Graphics;

				using (var bkBrush = new SolidBrush(e.BackColor))
				{
					g.FillRectangle(bkBrush, e.Bounds);
				}
				if (e.Index >= 0) // Index was observed to be -1 once in a while!
				{
					var li = listBox.Items[e.Index] as ListItem;
					if (li != null)
					{
						var bmkImg = StateInspectorResources.Bookmark;
						var bmkImgSz = bmkImg.GetSize(width: UIUtils.Dpi.Scale(9));

						bool isBookmarked = viewEvents.OnGetHistoryItemBookmarked(li.evt);
						if (isBookmarked)
						{
							var r = listBox.GetItemRectangle(e.Index);
							g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
							g.DrawImage(bmkImg, new RectangleF(1, r.Y + (r.Height - bmkImgSz.Height) / 2, bmkImgSz.Width, bmkImgSz.Height));
						}

						var focusedMessage = viewEvents.OnDrawFocusedMessageMark();
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
							g.DrawString(li.ToString(), e.Font, b, e.Bounds.Location, strFormat);
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
				if (k != Key.None)
				{
					viewEvents.OnChangeHistoryItemKeyEvent(PrimarySelectedItem, k);
					return false;
				}
			}
			return base.ProcessDialogKey(keyData);
		}
	}
}
