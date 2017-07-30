using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.SearchEditorDialog
{
	public class Presenter : IPresenter, IDialogViewEvents
	{
		readonly IView view;
		readonly FiltersManagerFactory filtersManagerFactory;
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;

		IDialogView dialogView;
		Func<bool> confirm; 

		public delegate FiltersManager.IPresenter FiltersManagerFactory(IFiltersList filtersList, IDialogView dialogView);

		public Presenter(
			IView view,
			IUserDefinedSearches userDefinedSearches,
			FiltersManagerFactory filtersManagerFactory,
			IAlertPopup alerts
		)
		{
			this.view = view;
			this.userDefinedSearches = userDefinedSearches;
			this.filtersManagerFactory = filtersManagerFactory;
			this.alerts = alerts;
		}

		void IPresenter.Open(IUserDefinedSearch search)
		{
			IFiltersList tempList = search.Filters.Clone();
			using (dialogView = view.CreateDialog(this))
			using (var filtersManagerPresenter = filtersManagerFactory(tempList, dialogView))
			{
				dialogView.SetData(new DialogData()
				{
					Name = search.Name
				});
				bool confirmed = false;
				confirm = () =>
				{
					string name = dialogView.GetData().Name;
					if (string.IsNullOrWhiteSpace(name))
					{
						alerts.ShowPopup(
							"Search editor", 
							"Bad search name.",
							AlertFlags.Ok
						);
						return false;
					}
					if (name != search.Name && userDefinedSearches.ContainsItem(name))
					{
						alerts.ShowPopup(
							"Search editor", 
							string.Format("Name '{0}' is already used by another search. Enter another name.", name), 
							AlertFlags.Ok
						);
						return false;
					}
					if (tempList.Count == 0)
					{
						// todo: alert
						return false;
					}
					confirmed = true;
					return confirmed;
				};
				dialogView.OpenModal();
				confirm = null;
				if (confirmed)
				{
					search.Name = dialogView.GetData().Name;
					search.Filters = tempList;
				}
				else
				{
					tempList.Dispose();
				}
			}
			dialogView = null;
		}

		void IDialogViewEvents.OnConfirmed ()
		{
			if (confirm())
				dialogView.CloseModal();
		}

		void IDialogViewEvents.OnCancelled ()
		{
			dialogView.CloseModal();
		}
	};
};