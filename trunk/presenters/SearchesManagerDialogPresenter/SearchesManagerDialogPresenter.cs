using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public class Presenter : IPresenter, IDialogViewEvents
	{
		readonly IView view;
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly SearchEditorDialog.IPresenter searchEditorDialog;

		IDialogView currentDialog;

		public Presenter(
			IView view,
			IUserDefinedSearches userDefinedSearches,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			SearchEditorDialog.IPresenter searchEditorDialog
		)
		{
			this.view = view;
			this.userDefinedSearches = userDefinedSearches;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.searchEditorDialog = searchEditorDialog;

			userDefinedSearches.OnChanged += (sender, e) => 
			{
				if (currentDialog == null)
					return;
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
			{
				userDefinedSearches.Delete(search);
			}
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
			var selection = GetSelection().ToArray();
			if (selection.Length == 0)
				return;
			var fileName = fileDialogs.SaveFileDialog(new SaveFileDialogParams()
			{
				Title = string.Format("Exporting {0} searche(s)", selection.Length),
				SuggestedFileName = "my_searches.xml"
			});
			if (fileName == null)
			{
				return;
			}
			using (var fs = new FileStream(fileName, FileMode.Create))
			{
				userDefinedSearches.Export(selection, fs);
			}
		}

		void IDialogViewEvents.OnImportClicked()
		{
			var fileName = fileDialogs.OpenFileDialog(new OpenFileDialogParams()
			{
				AllowsMultipleSelection = false,
				CanChooseFiles = true,
				CanChooseDirectories = false
			});
			if (fileName == null || fileName.Length == 0)
			{
				return;
			}
			using (var fs = new FileStream(fileName[0], FileMode.Open))
			{
				userDefinedSearches.Import(fs, dupeName =>
				{
					var userSelection = alerts.ShowPopup(
						"Import",
						string.Format("Search with name '{0}' already exists. Overwrite?", dupeName),
						AlertFlags.YesNoCancel
					);
					if (userSelection == AlertFlags.Cancel)
						return NameDuplicateResolution.Cancel;
					if (userSelection == AlertFlags.Yes)
						return NameDuplicateResolution.Overwrite;
					return NameDuplicateResolution.Skip;
				});
			}
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