using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.BookmarksList;
using System.Drawing.Drawing2D;

namespace LogJoint.UI
{
    public partial class BookmarksView : UserControl
    {
        public BookmarksView()
        {
            InitializeComponent();

            linkDisplayFont = new Font(listBox.Font, FontStyle.Underline);
            timeDeltaDisplayFont = listBox.Font;

            bookmarkIcon = Properties.Resources.Bookmark;
            bookmarkIconSize = Drawing.PrimitivesExtensions.ToSystemDrawingObject(bookmarkIcon.GetSize(width: UIUtils.Dpi.Scale(13f)));

            displayStringFormat = new StringFormat();
            displayStringFormat.Alignment = StringAlignment.Near;
            displayStringFormat.LineAlignment = StringAlignment.Near;
            displayStringFormat.Trimming = StringTrimming.EllipsisCharacter;
            displayStringFormat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoFontFallback;

            listBox.ItemHeight = UIUtils.Dpi.Scale(15);
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.presenter = viewModel;

            if (viewModel.FontName != null && (LogJoint.Properties.Settings.Default.MonospaceBookmarks ?? "") == "1")
            {
                linkDisplayFont.Dispose();
                linkDisplayFont = new Font(viewModel.FontName, 8f, FontStyle.Underline);
            }

            var itemsUpdater = Updaters.Create(
                () => viewModel.Items,
                (items) =>
                {
                    metrics = null;
                    isUpdating = true;
                    listBox.BeginUpdate();
                    listBox.SelectedIndices.Clear();
                    if (items.Count == listBox.Items.Count) // special case optimization
                    {
                        var itemIdx = 0;
                        foreach (var i in items)
                        {
                            listBox.Items[itemIdx] = i;
                            if (i.IsSelected)
                                listBox.SelectedIndices.Add(itemIdx);
                            ++itemIdx;
                        }
                    }
                    else
                    {
                        listBox.Items.Clear();
                        foreach (var i in items)
                        {
                            var itemIdx = listBox.Items.Add(i);
                            if (i.IsSelected)
                                listBox.SelectedIndices.Add(itemIdx);
                        }
                    }
                    listBox.EndUpdate();
                    isUpdating = false;
                }
            );
            var focusedMessageMarkUpdater = Updaters.Create(
                () => viewModel.FocusedMessagePosition,
                _ =>
                {
                    var focusedItemMarkBounds = UIUtils.FocusedItemMarkBounds;
                    listBox.Invalidate(new Rectangle(
                        GetMetrics().FocusedMessageMarkX + (int)focusedItemMarkBounds.Left,
                        0,
                        (int)focusedItemMarkBounds.Width,
                        ClientSize.Height));
                }
            );
            viewModel.ChangeNotification.CreateSubscription(() =>
            {
                itemsUpdater();
                focusedMessageMarkUpdater();
            });
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        int? GetLinkFromPoint(int x, int y, bool fullRowMode, bool enabledOnly)
        {
            if (listBox.Items.Count == 0)
                return null;
            y -= listBox.GetItemRectangle(0).Top;
            int idx = y / listBox.ItemHeight;
            if (idx < 0 || idx >= listBox.Items.Count)
                return null;
            if (!fullRowMode)
            {
                var txt = listBox.Items[idx].ToString();
                using (var g = listBox.CreateGraphics())
                {
                    var m = GetMetrics();
                    if (x < m.TextX || x > m.TextX + g.MeasureString(txt, linkDisplayFont, listBox.ClientSize.Width - m.TextX, displayStringFormat).Width)
                        return null;
                }
            }
            var item = GetItem(idx);
            if (item == null)
                return null;
            if (enabledOnly && !item.IsEnabled)
                return null;
            return idx;
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            var pt = listBox.PointToClient(Control.MousePosition);
            if (GetLinkFromPoint(pt.X, pt.Y, true, true) != null)
                presenter.OnViewDoubleClicked();
        }

        class Metrics
        {
            public int DeltaStringX;
            public int DeltaStringWidth;
            public int IconX;
            public int FocusedMessageMarkX;
            public int TextX;
        };

        Metrics CreateMetrics()
        {
            using (var g = this.CreateGraphics())
            {
                var m = new Metrics();
                m.DeltaStringX = 1;

                m.DeltaStringWidth = (int)
                     EnumItems()
                    .Select(i => Math.Max(
                        g.MeasureString(i.Delta ?? "", timeDeltaDisplayFont, new PointF(), displayStringFormat).Width,
                        g.MeasureString(i.AltDelta ?? "", timeDeltaDisplayFont, new PointF(), displayStringFormat).Width
                    ))
                    .Union(Enumerable.Repeat(0f, 1))
                    .Max() + 2;

                m.IconX = m.DeltaStringX + m.DeltaStringWidth;
                m.FocusedMessageMarkX = m.IconX + (int)bookmarkIconSize.Width + 1;
                m.TextX = m.FocusedMessageMarkX + 4;

                return m;
            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            var item = GetItem(e.Index);
            if (item == null)
            {
                e.Graphics.FillRectangle(Brushes.White, e.Bounds);
                return; // DrawItem sometimes called even when no item in the list :(
            }

            // use double buffering to ensure good look over RDP session.
            // direct drawing to e.Graphics in DRP session disables font smoothing for some reason.
            using (var backBuffer = bufferedGraphicsContext.Allocate(e.Graphics, e.Bounds))
            {
                var g = backBuffer.Graphics;

                Brush bkBrush = Brushes.White;

                if ((e.State & DrawItemState.Selected) != 0)
                {
                    bkBrush = selectedBkBrush;
                }
                else
                {
                    if (item.ContextColor != null)
                        bkBrush = UIUtils.GetPaletteColorBrush(item.ContextColor.Value);
                }

                g.FillRectangle(bkBrush, e.Bounds);

                var m = GetMetrics();

                Rectangle r = e.Bounds;
                r.X = m.DeltaStringX;
                r.Width = m.DeltaStringWidth;

                var deltaStr = item.Delta;
                if (deltaStr != null)
                {
                    g.DrawString(
                        deltaStr,
                        timeDeltaDisplayFont,
                        Brushes.Black,
                        r,
                        displayStringFormat);
                }

                g.InterpolationMode = InterpolationMode.High;
                g.DrawImage(bookmarkIcon,
                    e.Bounds.X + m.IconX,
                    e.Bounds.Y + (e.Bounds.Height - bookmarkIconSize.Height) / 2,
                    bookmarkIconSize.Width,
                    bookmarkIconSize.Height
                );

                r.X = m.TextX;
                r.Width = ClientSize.Width - m.TextX;
                g.DrawString(item.Text, linkDisplayFont,
                    item.IsEnabled ? Brushes.Blue : Brushes.Gray, r, displayStringFormat);
                if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.Focus) != 0)
                {
                    ControlPaint.DrawFocusRectangle(g, r, Color.Black, Color.White);
                }
                var focused = presenter.FocusedMessagePosition;
                if (focused != null)
                {
                    float y;
                    if (focused.LowerBound != focused.UpperBound)
                        y = listBox.ItemHeight * focused.LowerBound + listBox.ItemHeight / 2;
                    else
                        y = listBox.ItemHeight * focused.LowerBound;
                    if (y == 0)
                        y = UIUtils.FocusedItemMarkBounds.Height / 2;
                    y -= listBox.TopIndex * listBox.ItemHeight;
                    UIUtils.DrawFocusedItemMark(g, metrics.FocusedMessageMarkX, y);
                }

                backBuffer.Render(e.Graphics);
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                presenter.OnEnterKeyPressed();
            else if (e.KeyCode == Keys.C && e.Control)
                presenter.OnCopyShortcutPressed();
            else if (e.KeyCode == Keys.Insert && e.Control)
                presenter.OnCopyShortcutPressed();
            else if (e.KeyCode == Keys.Delete)
                presenter.OnDeleteButtonPressed();
            else if (e.KeyCode == Keys.A && e.Control)
                presenter.OnSelectAllShortcutPressed();
        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            bool leftClick = e.Button == MouseButtons.Left;
            bool rightClick = e.Button == MouseButtons.Right;
            int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false, enabledOnly: leftClick);
            if (linkUnderMouse != null)
            {
                if (leftClick)
                {
                    var item = Get(linkUnderMouse.Value);
                    if (item != null)
                        presenter.OnBookmarkLeftClicked(item);
                }
                else if (rightClick)
                {
                    listBox.SelectedIndex = linkUnderMouse.Value;
                }
            }
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false, true);
            listBox.Cursor = linkUnderMouse.HasValue ? Cursors.Hand : Cursors.Default;
        }

        IEnumerable<IViewItem> EnumItems()
        {
            return Enumerable.Range(0, listBox.Items.Count).Select(GetItem);
        }

        IViewItem GetItem(int index)
        {
            if (index >= 0 && index < listBox.Items.Count)
                return listBox.Items[index] as IViewItem;
            return null;
        }

        IViewItem Get(int index)
        {
            return GetItem(index);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            presenter.OnMenuItemClicked(ContextMenuItem.Delete);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            presenter.OnMenuItemClicked(ContextMenuItem.Copy);
        }

        private void copyWithTimeDeltasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            presenter.OnMenuItemClicked(ContextMenuItem.CopyWithDeltas);
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            ContextMenuItem items = presenter.OnContextMenu();
            if (items == ContextMenuItem.None)
            {
                e.Cancel = true;
                return;
            }
            deleteToolStripMenuItem.Visible = (items & ContextMenuItem.Delete) != 0;
            copyToolStripMenuItem.Visible = (items & ContextMenuItem.Copy) != 0;
            copyWithTimeDeltasToolStripMenuItem.Visible = (items & ContextMenuItem.CopyWithDeltas) != 0;
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isUpdating)
                return;
            var first = listBox.SelectedIndex;
            var selected = new[] { Get(first) }.Union(
                listBox.SelectedIndices.OfType<int>().Where(i => i != first).Select(Get)
            ).Where(i => i != null).ToArray();
            presenter.OnChangeSelection(selected);
        }

        Metrics GetMetrics()
        {
            if (metrics == null)
                metrics = CreateMetrics();
            return metrics;
        }

        private IViewModel presenter;
        private Font timeDeltaDisplayFont;
        private Font linkDisplayFont;
        private StringFormat displayStringFormat;
        private Metrics metrics;
        private Brush selectedBkBrush = new SolidBrush(Color.FromArgb(197, 206, 231));
        private bool isUpdating;
        private Bitmap bookmarkIcon;
        private SizeF bookmarkIconSize;
        private BufferedGraphicsContext bufferedGraphicsContext = new BufferedGraphicsContext();
    }

}
