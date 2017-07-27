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
				if (clearSearchIconSet)
					this.Text = "";
				else if (CanFocus)
					Focus();
			};
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SelectEnd()
		{
			this.Select(this.Text.Length, 0);
		}

		void IView.ReceiveInputFocus()
		{
			if (this.CanFocus)
				this.Focus();
		}

		void IView.ResetQuickSearchTimer(int due)
		{
			if (realtimeSearchTimer == null)
			{
				realtimeSearchTimer = new Timer() { Interval = 500 };
				realtimeSearchTimer.Tick += (timer, timerEvt) =>
				{
					realtimeSearchTimer.Enabled = false;
					viewEvents.OnQuickSearchTimerTriggered();
				};
			}
			realtimeSearchTimer.Enabled = false;
			realtimeSearchTimer.Enabled = true;
		}

		string IView.Text
		{
			get { return base.Text; }
			set { base.Text = value; }
		}

		void IView.SetListAvailability(bool value)
		{
			if (!value && suggestionsList == null)
				return;
			EnsureSuggestionsList();
			// todo: display/hide dropdown button
		}

		void IView.SetListVisibility(bool value)
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
		}

		void IView.SetListItems(List<ViewListItem> items)
		{
			EnsureSuggestionsList();
			suggestionsList.Items.Clear();
			suggestionsList.Items.AddRange(items.Select(i => new SuggestionsListItem()
			{
				PresentationObject = i
			}).ToArray());
		}

		void IView.SetListSelectedItem(int index)
		{
			EnsureSuggestionsList();
			suggestionsList.SelectedIndex = index;
		}

		void IView.RestrictTextEditing(bool restrict)
		{
			editingRestricted = restrict;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			viewEvents.OnLostFocus();
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
				viewEvents.OnKeyDown(key);
				return;
			}
			base.OnPreviewKeyDown(e);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyCode)
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
				viewEvents.OnKeyDown(key);
				return true; // this suppresses "ding" sound on ENTER
			}
			if (editingRestricted)
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
			if (prevText != this.Text)
			{
				viewEvents.OnTextChanged();
				prevText = this.Text;
			}

			bool needToSetClearSearchIcon = this.Text != "";
			if (clearSearchIconSet != needToSetClearSearchIcon)
			{
				this.picture.Image = needToSetClearSearchIcon ?
					QuickSearchTextBoxResources.close_16x16 : QuickSearchTextBoxResources.search_small;
				clearSearchIconSet = needToSetClearSearchIcon;
			}

			base.OnTextChanged(e);
		}

		protected override async void OnResize(EventArgs e)
		{
			base.OnResize(e);

			var EM_SETMARGINS = 0xd3;
			var EC_RIGHTMARGIN = (IntPtr)2;
			SendMessage(this.Handle, EM_SETMARGINS, EC_RIGHTMARGIN, (IntPtr)((this.Height + 2) << 16));

			await Task.Yield();
			LocatePicture();
		}

		private void LocatePicture()
		{
			int padding = BorderStyle == BorderStyle.FixedSingle ? 2 : 0;
			picture.Size = new Size(this.Height - 2 - padding, this.Height - 2 - padding);
			picture.Location = new Point(this.Width - picture.Size.Width - padding, 1 + padding);
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
				var item = suggestionsList.Items.Cast<SuggestionsListItem>().ElementAtOrDefault(e.Index);
				if (item == null)
					return;

				using (var b = new SolidBrush(e.ForeColor))
					e.Graphics.DrawString(item.PresentationObject.Text ?? "", this.Font,
						item.PresentationObject.IsSelectable ? b : Brushes.LightGray, e.Bounds);

				var lnk = item.PresentationObject.LinkText;
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
						var item = (SuggestionsListItem)suggestionsList.Items[i];
						if (item.PresentationObject.LinkText != null)
						{
							using (var g = CreateGraphics())
							{
								var linkW = (int)g.MeasureString(item.PresentationObject.LinkText, this.Font).Width;
								if (new Rectangle(r.Right - linkW, r.Y, linkW, r.Height).Contains(e.Location))
								{
									viewEvents.OnSuggestionLinkClicked(i);
									break;
								}
							}
						}
						viewEvents.OnSuggestionClicked(i);
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

		class SuggestionsListItem
		{
			public ViewListItem PresentationObject;
		};

		class SuggestionsList:  ListBox
		{
		};

		PictureBox picture = new PictureBox();
		bool clearSearchIconSet;
		IViewEvents viewEvents;
		Timer realtimeSearchTimer;
		ListBox suggestionsList;
		bool editingRestricted;
		string prevText;
	}
}
