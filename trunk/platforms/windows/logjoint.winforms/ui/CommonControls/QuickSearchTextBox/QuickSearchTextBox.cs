using LogJoint.UI.Presenters.QuickSearchTextBox;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LogJoint.UI.QuickSearchTextBox
{
	public partial class QuickSearchTextBox : TextBox, IView
	{
		public QuickSearchTextBox()
		{
			InitializeComponent();

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


		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				viewEvents.OnEscapePressed();
				return true;
			}
			else if (keyData == Keys.Enter)
			{
				viewEvents.OnEnterPressed();
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}

		protected override void OnTextChanged(EventArgs e)
		{
			viewEvents.OnTextChanged();

			bool needToSetClearSearchIcon = this.Text != "";
			if (clearSearchIconSet != needToSetClearSearchIcon)
			{
				this.picture.Image = needToSetClearSearchIcon ? 
					QuickSearchTextBoxResources.close_16x16 : QuickSearchTextBoxResources.search_small;
				clearSearchIconSet = needToSetClearSearchIcon;
			}

			base.OnTextChanged(e);
		}

		protected override void OnResize(EventArgs e)
		{
			var EM_SETMARGINS = 0xd3;
			var EC_RIGHTMARGIN = (IntPtr)2;
			//SendMessage(this.Handle, EM_SETMARGINS, EC_RIGHTMARGIN, (IntPtr)((this.Height + 2) << 16));

			int padding = BorderStyle == BorderStyle.FixedSingle ? 2 : 0;
			picture.Size = new Size(this.Height - 2 - padding, this.Height - 2 - padding);
			picture.Location = new Point(this.Width - picture.Size.Width - padding, 1 + padding);

			base.OnResize(e);
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		PictureBox picture = new PictureBox();
		bool clearSearchIconSet;
		IViewEvents viewEvents;
		Timer realtimeSearchTimer;
	}
}
