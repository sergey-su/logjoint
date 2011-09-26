using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	class LogJointApplication: ILogJointApplication
	{
		public LogJointApplication(Model model, UI.MainForm mainForm, UI.LogViewerControl view)
		{
			this.model = model;
			this.mainForm = mainForm;
			this.view = view;
		}

		public void FireFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		public void FireSourcesChanged()
		{
			if (SourcesChanged != null)
				SourcesChanged(this, EventArgs.Empty);
		}

		#region ILogJointApplication Members

		public Model Model
		{
			get { return model; }
		}

		public void RegisterToolForm(Form f)
		{
			mainForm.AddOwnedForm(f);
		}

		public void ShowFilter(Filter f)
		{
			mainForm.menuTabControl.SelectedTab = mainForm.filtersTabPage;
			mainForm.displayFiltersListView.SelectFilter(f);
		}

		public MessageBase FocusedMessage
		{
			get { return view.Presenter.FocusedMessage; }
		}

		public IMessagesCollection LoadedMessagesCollection
		{
			get { return view.Presenter.LoadedMessagesCollection; }
		}

		public void SelectMessageAt(IBookmark bmk, Predicate<MessageBase> messageMatcherWhenNoHashIsSpecified)
		{
			view.Presenter.SelectMessageAt(bmk, messageMatcherWhenNoHashIsSpecified);
		}

		public event EventHandler FocusedMessageChanged;
		public event EventHandler SourcesChanged;

		#endregion

		Model model;
		UI.MainForm mainForm;
		UI.LogViewerControl view;
	}
}
