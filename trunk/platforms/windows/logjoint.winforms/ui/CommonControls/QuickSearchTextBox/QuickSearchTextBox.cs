using LogJoint.UI.Presenters.QuickSearchTextBox;
using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace LogJoint.UI.QuickSearchTextBox
{
	public partial class QuickSearchTextBox : TextBox, IView
	{
		public QuickSearchTextBox()
		{
			InitializeComponent();

			this.components = new System.ComponentModel.Container();

			this.Multiline = false;

			this.Controls.Add(picture);
			this.picture.Cursor = Cursors.Default;
			this.picture.SizeMode = PictureBoxSizeMode.StretchImage;
			this.picture.Image = QuickSearchTextBoxResources.search_small;
			this.picture.Click += (s, e) =>
			{
				if (viewModel.ClearTextIconVisible)
					viewModel.OnClearTextIconClicked();
				if (CanFocus)
					Focus();
			};

			this.Controls.Add(dropDownButton);
			this.dropDownButton.Visible = false;
			this.dropDownButton.Cursor = Cursors.Default;
			this.dropDownButton.Text = "";
			this.dropDownButton.FlatStyle = FlatStyle.Flat;
			this.dropDownButton.FlatAppearance.BorderColor = Color.Gray;
			this.dropDownButton.BackColor = this.BackColor;
			this.dropDownButton.Padding = new Padding();
			this.dropDownButton.Margin = new Padding();
			this.dropDownButton.Paint += (s, e) =>
			{
				var r = dropDownButton.ClientRectangle;
				var signSz = r.Height / 2f;

				var state = e.Graphics.Save();
				e.Graphics.TranslateTransform(r.Width / 2, r.Height / 2);
				if (suggestionsList.Visible)
					e.Graphics.ScaleTransform(1, -1);
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				var h = signSz * 0.866f;
				e.Graphics.FillPolygon(Brushes.Black, new PointF[]
				{
					new PointF(-signSz/2, -h/2),
					new PointF(signSz/2, -h/2),
					new PointF(0, h/2),
				});
				e.Graphics.Restore(state);
			};
			this.dropDownButton.Click += (s, e) =>
			{
				viewModel.OnDropDownButtonClicked();
			};
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateListAvailability = Updaters.Create(
				() => viewModel.SuggestionsListAvailable,
				SetListAvailability
			);
			var updateListVisibility = Updaters.Create(
				() => viewModel.SuggestionsListVisibile,
				SetListVisibility
			);
			var updateList = Updaters.Create(
				() => viewModel.SuggestionsListItems,
				() => viewModel.SuggestionsListAvailable,
				(items, available) =>
				{
					if (available && viewModel.SuggestionsListContentVersion != listVersion)
					{
						listVersion = viewModel.SuggestionsListContentVersion;
						SetListItems(items);
					}
				}
			);
			var updateSelectedListItem = Updaters.Create(
				() => viewModel.SelectedSuggestionsListItem,
				SetListSelectedItem
			);
			var udateText = Updaters.Create(
				() => viewModel.Text,
				value =>
				{
					using (new ScopedGuard(() => lockTextChange = true, () => lockTextChange = false))
						base.Text = value;
				}
			);
			var updateClearIcon = Updaters.Create(
				() => viewModel.ClearTextIconVisible,
				value =>
				{
					picture.Image = value ?
						QuickSearchTextBoxResources.close_16x16 : QuickSearchTextBoxResources.search_small;
				}
			);
			subscription = viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateListAvailability();
				updateListVisibility();
				updateList();
				updateSelectedListItem();
				udateText();
				updateClearIcon();
			});
		}

		void IView.SelectEnd()
		{
			this.Select(this.Text.Length, 0);
		}

		void IView.SelectAll()
		{
			base.SelectAll();
		}

		void IView.ReceiveInputFocus()
		{
			if (this.CanFocus)
				this.Focus();
		}

		void SetListAvailability(bool value)
		{
			if (!value && suggestionsList == null)
				return;
			EnsureSuggestionsList();
			dropDownButton.Visible = value;
			LayoutChildren();
		}

		void SetListVisibility(bool value)
		{
			if (!value && suggestionsList == null)
				return;
			EnsureSuggestionsList();
			suggestionsList.Visible = value;
			if (value)
			{
				UpdateSuggestionsListBounds();
				suggestionsList.BringToFront();
			}
			dropDownButton.Invalidate();
		}

		void SetListItems(IReadOnlyList<ISuggestionsListItem> items)
		{
			EnsureSuggestionsList();
			suggestionsList.Items.Clear();
			suggestionsList.Items.AddRange(items.ToArray());
		}

		void SetListSelectedItem(int? index)
		{
			if (index == null && suggestionsList == null)
				return;
			EnsureSuggestionsList();
			suggestionsList.SelectedIndex = index.GetValueOrDefault(-1);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			viewModel.OnLostFocus();
			base.OnLostFocus(e);
		}

		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			var keyCode = e.KeyCode;
			var key = Key.None;
			if (keyCode == Keys.Down && (ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
				key = Key.ShowListShortcut;
			else if (keyCode == Keys.Up && (ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
				key = Key.HideListShortcut;
			if (key != Key.None)
			{
				viewModel.OnKeyDown(key);
				return;
			}
			base.OnPreviewKeyDown(e);
		}

		protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyCode)
		{
			var key = Key.None;
			if (keyCode == Keys.Down)
				key = Key.Down;
			else if (keyCode == Keys.Up)
				key = Key.Up;
			if (keyCode == Keys.PageDown)
				key = Key.PgDown;
			else if (keyCode == Keys.PageUp)
				key = Key.PgUp;
			else if (keyCode == Keys.Escape)
				key = Key.Escape;
			else if (keyCode == Keys.Enter)
				key = Key.Enter;
			if (key != Key.None) 
			{
				viewModel.OnKeyDown(key);
				return true; // this suppresses "ding" sound on ENTER
			}
			if (viewModel.TextEditingRestricted)
			{
				bool allowChange =
					// text navigation
					keyCode == Keys.Left || keyCode == Keys.Right ||
					keyCode == Keys.Home || keyCode == Keys.End ||
					(keyCode == (Keys.A | Keys.Control)) ||
					(keyCode == (Keys.Home | Keys.Shift)) ||
					(keyCode == (Keys.End | Keys.Shift)) ||
					
					// std shortcut to close parent window
					(keyCode == (Keys.F4 | Keys.Alt)) ||
					
					// the keys deleting all
					(keyCode == Keys.Back || keyCode == Keys.Delete) && SelectedText == Text;
				if (!allowChange)
				{
					return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyCode);
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (!lockTextChange && prevText != this.Text)
			{
				viewModel.OnChangeText(this.Text);
				prevText = this.Text;
			}
			base.OnTextChanged(e);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			LayoutChildren();
		}

		private async void LayoutChildren()
		{
			await Task.Yield();

			var cliSz = this.ClientSize;
			var childSz = cliSz.Height - 4;
			int margin = 0;

			var childY = (cliSz.Height - childSz) / 2;
			var childX = cliSz.Width - cliSz.Height + (cliSz.Height - childSz) / 2;

			if (viewModel.SuggestionsListAvailable)
			{
				dropDownButton.Size = new Size(childSz, childSz);
				dropDownButton.Location = new Point(childX, childY);
				childX -= cliSz.Height;
				margin += cliSz.Height;
			}

			picture.Size = new Size(childSz, childSz);
			picture.Location = new Point(childX, childY);
			margin += cliSz.Height;

			var EM_SETMARGINS = 0xd3;
			var EC_RIGHTMARGIN = (IntPtr)2;
			SendMessage(this.Handle, EM_SETMARGINS, EC_RIGHTMARGIN, (IntPtr)((margin + 2) << 16));
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		int GetSuggestionsListItemHeight()
		{
			using (var g = this.CreateGraphics())
				return (int)(g.MeasureString("Foobar123", this.Font).Height + 2);
		}

		void EnsureSuggestionsList()
		{
			if (suggestionsList != null)
				return;
			suggestionsList = new SuggestionsList()
			{
				Visible = false,
				BorderStyle = BorderStyle.FixedSingle,
				DrawMode = DrawMode.OwnerDrawFixed,
				ItemHeight = GetSuggestionsListItemHeight(),
			};
			components.Add(suggestionsList);
			var form = FindForm();
			if (form == null)
				throw new InvalidOperationException("can not use suggestions list on detached search textbox");
			suggestionsList.Parent = form;
			form.Resize += (s, e) => UpdateSuggestionsListBounds();
			suggestionsList.DrawItem += (s, e) =>
			{
				e.DrawBackground();
				var item = suggestionsList.Items.Cast<ISuggestionsListItem>().ElementAtOrDefault(e.Index);
				if (item == null)
					return;

				using (var b = new SolidBrush(e.ForeColor))
					e.Graphics.DrawString(item.Text ?? "", this.Font,
						item.IsSelectable ? b : Brushes.LightGray, e.Bounds);

				var lnk = item.LinkText;
				if (!string.IsNullOrEmpty(lnk))
				{
					using (var sf = new StringFormat() { Alignment = StringAlignment.Far })
					using (var fnt = new Font(this.Font, FontStyle.Underline))
					{
						e.Graphics.DrawString(lnk, fnt, Brushes.Blue, e.Bounds.Right, e.Bounds.Top, sf);
					}
				}
			};
			suggestionsList.MouseDown += (s, e) =>
			{
				for (int i = 0; i < suggestionsList.Items.Count; ++i)
				{
					var r = suggestionsList.GetItemRectangle(i);
					if (r.Contains(e.Location))
					{
						var item = (ISuggestionsListItem)suggestionsList.Items[i];
						if (item.LinkText != null)
						{
							using (var g = CreateGraphics())
							{
								var linkW = (int)g.MeasureString(item.LinkText, this.Font).Width;
								if (new Rectangle(r.Right - linkW, r.Y, linkW, r.Height).Contains(e.Location))
								{
									viewModel.OnSuggestionLinkClicked(i);
									break;
								}
							}
						}
						viewModel.OnSuggestionClicked(i);
						break;
					}
				}
			};
		}

		void UpdateSuggestionsListBounds()
		{
			var parent = suggestionsList.Parent;
			var editorRect = parent.RectangleToClient(this.RectangleToScreen(this.ClientRectangle));
			suggestionsList.SetBounds(
				editorRect.X, editorRect.Bottom,
				editorRect.Width, Math.Max(1, parent.ClientSize.Height - editorRect.Bottom - 20)
			);
		}

		class SuggestionsList:  ListBox
		{
		};

		class DropDownButton : Button
		{
			public DropDownButton()
			{
				SetStyle(ControlStyles.Selectable, false);
			}
		};

		readonly PictureBox picture = new PictureBox();
		readonly Button dropDownButton = new DropDownButton();
		IViewModel viewModel;
		ISubscription subscription;
		ListBox suggestionsList;
		string prevText;
		int? listVersion;
		bool lockTextChange;
	}
}
