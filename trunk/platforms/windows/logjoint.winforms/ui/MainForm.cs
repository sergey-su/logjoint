using System;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.MainForm;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class MainForm : Form, IView, IWinFormsComponentsInitializer
	{
		IViewEvents presenter;

		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			presenter.OnClosing();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			presenter.OnLoad();
		}

		protected override bool ProcessTabKey(bool forward)
		{
			presenter.OnTabPressed();
			return base.ProcessTabKey(forward);
		}

		private void cancelLongRunningProcessDropDownButton_Click(object sender, EventArgs e)
		{
			presenter.OnCancelLongRunningProcessButtonClicked();
		}

		private void MainForm_KeyDown(object se, KeyEventArgs e)
		{
			KeyCode key = KeyCode.Unknown;

			Keys keyCode = e.KeyData & Keys.KeyCode;
			if (keyCode == Keys.Escape)
				key = KeyCode.Escape;
			else if (keyCode == Keys.F)
				key = KeyCode.F;
			else if (keyCode == Keys.K)
				key = KeyCode.K;
			else if (keyCode == Keys.F3)
				key = KeyCode.F3;
			else if (keyCode == Keys.F2)
				key = KeyCode.F2;

			if (key != KeyCode.Unknown)
				presenter.OnKeyPressed(key, (e.KeyData & Keys.Shift) != 0, (e.KeyData & Keys.Control) != 0);
		}

		private void optionsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnOptionsLinkClicked();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnAboutMenuClicked();
		}

		private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnConfigurationMenuClicked();
		}

		private void MainForm_DragOver(object sender, DragEventArgs e)
		{
			if (presenter.OnDragOver(e.Data))
				e.Effect = DragDropEffects.All;
		}

		private void MainForm_DragDrop(object sender, DragEventArgs e)
		{
			presenter.OnDragDrop(e.Data);
		}

		private void rawViewToolStripButton_Click(object sender, EventArgs e)
		{
			presenter.OnRawViewButtonClicked();
		}

		void IWinFormsComponentsInitializer.InitOwnedForm(Form form)
		{
			components.Add(form);
			AddOwnedForm(form);
		}

		void Presenters.MainForm.IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.ShowOptionsMenu()
		{
			optionsContextMenu.Show(
				aboutLinkLabel.PointToScreen(new Point(0, aboutLinkLabel.Height)));
		}

		void IView.ShowAboutBox()
		{
			using (AboutBox aboutBox = new AboutBox())
			{
				aboutBox.ShowDialog();
			}
		}

		IInputFocusState IView.CaptureInputFocusState()
		{
			return new InputFocusState(this);
		}

		void IView.ExecuteThreadPropertiesDialog(IThread thread, IPresentersFacade navHandler)
		{
			using (UI.ThreadPropertiesForm f = new UI.ThreadPropertiesForm(thread, navHandler))
			{
				f.ShowDialog();
			}
		}

		void IView.SetCancelLongRunningControlsVisibility(bool value)
		{
			cancelLongRunningProcessLabel.Visible = value;
			cancelLongRunningProcessDropDownButton.Visible = value;
		}

		void IView.BeginSplittingSearchResults()
		{
			splitContainer_Log_SearchResults.BeginSplitting();
		}

		void IView.ActivateTab(string tabId)
		{
			var page = TabPageById(tabId);
			if (page != null)
				menuTabControl.SelectedTab = page;
		}

		void IView.EnableFormControls(bool enable)
		{
			splitContainer_Menu_Workspace.Enabled = enable;
			splitContainer_Menu_Workspace.ForeColor = !enable ? Color.Gray : Color.Black;
			Win32Native.EnableMenuItem(Win32Native.GetSystemMenu(this.Handle, false), Win32Native.SC_CLOSE,
				!enable ? Win32Native.MF_GRAYED : Win32Native.MF_ENABLED);
		}

		void IView.SetAnalizingIndicationVisibility(bool value)
		{
			toolStripAnalizingImage.Visible = value;
			toolStripAnalizingLabel.Visible = value;
		}

		void IView.SetCaption(string value)
		{
			this.Text = value;
		}

		TabPage TabPageById(string tabId)
		{
			switch (tabId)
			{
				case TabIDs.Sources: return sourcesTabPage;
				case TabIDs.Threads: return threadsTabPage;
				case TabIDs.DisplayFilteringRules: return filtersTabPage;
				case TabIDs.HighlightingFilteringRules: return highlightTabPage;
				case TabIDs.Bookmarks: return navigationTabPage;
				case TabIDs.Search: return searchTabPage;
				default: return null;
			}
		}

		class InputFocusState : IInputFocusState
		{
			public InputFocusState(MainForm form)
			{
				focusedControlBeforeWaitState = Control.FromHandle(Win32Native.GetFocus());

				if (focusedControlBeforeWaitState == null
				 && form.searchPanelView.searchTextBox.Focused)
				{
					// ComboBox's child EDIT returned by win32 GetFocus()
					// can not be found by Control.FromHandle().
					// Handle search box separately.
					focusedControlBeforeWaitState = form.searchPanelView.searchTextBox;
				}
			}

			void IInputFocusState.Restore()
			{
				if (focusedControlBeforeWaitState != null
				 && !focusedControlBeforeWaitState.IsDisposed
				 && focusedControlBeforeWaitState.Enabled
				 && focusedControlBeforeWaitState.CanFocus)
				{
					focusedControlBeforeWaitState.Focus();
				}
				focusedControlBeforeWaitState = null;
			}

			Control focusedControlBeforeWaitState;
		};

		static class Win32Native
		{
			public const int SC_CLOSE = 0xF060;
			public const int MF_GRAYED = 0x1;
			public const int MF_ENABLED = 0x0;

			[DllImport("user32.dll")]
			public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

			[DllImport("user32.dll")]
			public static extern int EnableMenuItem(IntPtr hMenu, int wIDEnableItem, int wEnable);

			[DllImport("user32.dll")]
			public static extern IntPtr GetFocus();

			[DllImport("user32.dll")]
			public static extern IntPtr GetParent(IntPtr hWnd);
		}
	}

}