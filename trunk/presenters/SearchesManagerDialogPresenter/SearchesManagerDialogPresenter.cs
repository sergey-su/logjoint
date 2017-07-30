using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public class Presenter : IPresenter, IDialogViewEvents
	{
		readonly IView view;
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;

		IDialogView dialogView;

		public Presenter(
			IView view,
			IUserDefinedSearches userDefinedSearches,
			IAlertPopup alerts
		)
		{
			this.view = view;
			this.userDefinedSearches = userDefinedSearches;
			this.alerts = alerts;
		}

		void IPresenter.Open()
		{
			using (dialogView = view.CreateDialog(this))
			{
				//dialogView.SetItems();
				dialogView.OpenModal();
			}
			dialogView = null;
		}

		void IDialogViewEvents.OnCloseClicked ()
		{
			dialogView.CloseModal();
		}

		void IDialogViewEvents.OnAddClicked()
		{
		}

		void IDialogViewEvents.OnDeleteClicked()
		{
		}

		void IDialogViewEvents.OnEditClicked()
		{
		}

		void IDialogViewEvents.OnSelectionChanged()
		{
		}
	};
};