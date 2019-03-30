using System;

namespace LogJoint.UI.Presenters.SearchEditorDialog
{
	public class Presenter : IPresenter, IDialogViewEvents
	{
		readonly IView view;
		readonly FiltersManagerFactory filtersManagerFactory;
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;
		const string alertsCaption = "Filter editor";

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

		bool IPresenter.Open(IUserDefinedSearch search)
		{
			IFiltersList tempList = search.Filters.Clone();
			bool confirmed = false;
			using (dialogView = view.CreateDialog(this))
			using (var filtersManagerPresenter = filtersManagerFactory(tempList, dialogView))
			{
				dialogView.SetData(new DialogData()
				{
					Name = search.Name
				});
				confirm = () =>
				{
					string name = dialogView.GetData().Name;
					if (string.IsNullOrWhiteSpace(name))
					{
						alerts.ShowPopup(
							alertsCaption, 
							"Bad filter name.",
							AlertFlags.Ok
						);
						return false;
					}
					if (name != search.Name && userDefinedSearches.ContainsItem(name))
					{
						alerts.ShowPopup(
							alertsCaption, 
							string.Format("Name '{0}' is already used by another filter. Enter another name.", name), 
							AlertFlags.Ok
						);
						return false;
					}
					if (tempList.Items.Count == 0)
					{
						alerts.ShowPopup(
							alertsCaption, 
							"Can not save: filter must have at least one rule.", 
							AlertFlags.Ok
						);
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
			return confirmed;
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