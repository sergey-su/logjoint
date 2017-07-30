using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public class Presenter : IPresenter, IDialogViewEvents
	{
		readonly IView view;
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;
		readonly SearchEditorDialog.IPresenter searchEditorDialog;

		IDialogView currentDialog;

		public Presenter(
			IView view,
			IUserDefinedSearches userDefinedSearches,
			IAlertPopup alerts,
			SearchEditorDialog.IPresenter searchEditorDialog
		)
		{
			this.view = view;
			this.userDefinedSearches = userDefinedSearches;
			this.alerts = alerts;
			this.searchEditorDialog = searchEditorDialog;

			userDefinedSearches.OnChanged += (sender, e) => 
			{
				UpdateViewItems();
				UpdateControls();
			};
		}

		void IPresenter.Open()
		{
			using (currentDialog = view.CreateDialog(this))
			{
				UpdateViewItems();
				UpdateControls();
				currentDialog.OpenModal();
			}
			currentDialog = null;
		}

		void IDialogViewEvents.OnCloseClicked ()
		{
			currentDialog.CloseModal();
		}

		void IDialogViewEvents.OnAddClicked()
		{
			var search = userDefinedSearches.AddNew();
			if (!searchEditorDialog.Open(search))
				userDefinedSearches.Delete(search);
		}

		void IDialogViewEvents.OnDeleteClicked()
		{
			var selected = GetSelection().ToArray();
			if (selected.Length == 0)
			{
				return;
			}
			if (alerts.ShowPopup(
				"Deletion",
                string.Format("Do you want to delete selected {0} item(s)?", selected.Length), 
                AlertFlags.YesNoCancel) != AlertFlags.Yes)
			{
				return;
			}
			foreach (var search in selected)
				userDefinedSearches.Delete(search);
		}

		void IDialogViewEvents.OnEditClicked()
		{
			var selected = GetSelection().FirstOrDefault();
			if (selected != null)
				searchEditorDialog.Open(selected);
		}

		void IDialogViewEvents.OnSelectionChanged()
		{
			UpdateControls();
		}

		void IDialogViewEvents.OnExportClicked()
		{
			// todo
		}

		void IDialogViewEvents.OnImportClicked()
		{
			// todo
		}

		IEnumerable<IUserDefinedSearch> GetSelection()
		{
			return currentDialog.SelectedItems.Select(i => i.Data)
				.OfType<IUserDefinedSearch>();
		}

		void UpdateViewItems()
		{
			currentDialog.SetItems(userDefinedSearches.Items.Select(i => new ViewItem()
			{
				Caption = i.Name,
				Data = i
			}).ToArray());
		}

		void UpdateControls()
		{
			var selection = GetSelection().Count();
			currentDialog.EnableControl(ViewControl.AddButton, true);
			currentDialog.EnableControl(ViewControl.Import, true);
			currentDialog.EnableControl(ViewControl.EditButton, selection == 1);
			currentDialog.EnableControl(ViewControl.DeleteButton, selection > 0);
			currentDialog.EnableControl(ViewControl.Export, selection > 0);
		}
	};
};