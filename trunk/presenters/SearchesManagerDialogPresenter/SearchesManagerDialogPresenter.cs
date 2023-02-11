using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Reactive;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly IUserDefinedSearches userDefinedSearches;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly SearchEditorDialog.IPresenter searchEditorDialog;
		readonly IChangeNotification changeNotification;

		TaskCompletionSource<IUserDefinedSearch> currentDialog;
		ImmutableList<ViewItem> viewItems = ImmutableList<ViewItem>.Empty;
		Func<IReadOnlySet<IUserDefinedSearch>> getSelection;
		Func<IReadOnlySet<ViewControl>> getEnabledControls;

		public Presenter(
			IUserDefinedSearches userDefinedSearches,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			SearchEditorDialog.IPresenter searchEditorDialog,
			IChangeNotification changeNotification
		)
		{
			this.userDefinedSearches = userDefinedSearches;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.searchEditorDialog = searchEditorDialog;
			this.changeNotification = changeNotification;

			getSelection = Selectors.Create(() => viewItems, items => 
				ImmutableHashSet.CreateRange(viewItems.Where(i => i.Selected).Select(i => i.Search)));
			getEnabledControls = Selectors.Create(getSelection, GetEnabledControls);

			userDefinedSearches.OnChanged += (sender, e) => 
			{
				if (currentDialog == null)
					return;
				UpdateViewItems(getSelection());
			};
		}

		Task<IUserDefinedSearch> IPresenter.Open()
		{
			Reset();
			UpdateViewItems(ImmutableHashSet<IUserDefinedSearch>.Empty);
			currentDialog = new TaskCompletionSource<IUserDefinedSearch>();
			changeNotification.Post();
			return currentDialog.Task;
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		bool IViewModel.IsVisible => currentDialog != null;
		string IViewModel.CloseButtonText => getSelection().Count == 1 ? "Use and close" : "Close";
		IReadOnlySet<ViewControl> IViewModel.EnabledControls => getEnabledControls();
		IReadOnlyList<IViewItem> IViewModel.Items => viewItems;

		void IViewModel.OnCloseClicked ()
		{
			var selection = getSelection();
			if (selection.Count == 1)
				currentDialog.SetResult(selection.First());
			Reset();
		}

		void IViewModel.OnCancelled() => Reset();

		async void IViewModel.OnAddClicked()
		{
			var search = userDefinedSearches.AddNew();
			if (!await searchEditorDialog.Open(search))
				userDefinedSearches.Delete(search);
		}

		async void IViewModel.OnDeleteClicked()
		{
			var selected = getSelection();
			if (selected.Count == 0)
			{
				return;
			}
			if (await alerts.ShowPopupAsync(
				"Deletion",
				string.Format("Do you want to delete selected {0} item(s)?", selected.Count), 
				AlertFlags.YesNoCancel) != AlertFlags.Yes)
			{
				return;
			}
			foreach (var search in selected)
			{
				userDefinedSearches.Delete(search);
			}
		}

		async void IViewModel.OnEditClicked()
		{
			var selected = getSelection().SingleOrDefault();
			if (selected != null)
				await searchEditorDialog.Open(selected);
		}

		void IViewModel.OnSelect(IEnumerable<IViewItem> requestedSelection)
		{
			UpdateViewItems(requestedSelection.OfType<ViewItem>().Select(i => i.Search).ToHashSet());
		}

		void IViewModel.OnExportClicked()
		{
			var selection = getSelection();
			if (selection.Count == 0)
				return;
			var fileName = fileDialogs.SaveFileDialog(new SaveFileDialogParams()
			{
				Title = string.Format("Exporting {0} search(es)", selection.Count),
				SuggestedFileName = "my_searches.xml"
			});
			if (fileName == null)
			{
				return;
			}
			using (var fs = new FileStream(fileName, FileMode.Create))
			{
				userDefinedSearches.Export(selection.ToArray(), fs);
			}
		}

		async void IViewModel.OnImportClicked()
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
			using var fs = new FileStream(fileName[0], FileMode.Open);
			await userDefinedSearches.Import(fs, async dupeName =>
			{
				var userSelection = await alerts.ShowPopupAsync(
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

		void UpdateViewItems(IReadOnlySet<IUserDefinedSearch> selection)
		{
			viewItems = ImmutableList.CreateRange(userDefinedSearches.Items.Select(
				search => new ViewItem(search, selected: selection.Contains(search))
			));
			changeNotification.Post();
		}

		static IReadOnlySet<ViewControl> GetEnabledControls(IReadOnlySet<IUserDefinedSearch> selection)
		{
			var builder = ImmutableHashSet.CreateBuilder<ViewControl>();
			builder.Add(ViewControl.AddButton);
			builder.Add(ViewControl.Import);
			if (selection.Count == 1)
				builder.Add(ViewControl.EditButton);
			if (selection.Count > 0)
				builder.Add(ViewControl.DeleteButton);
			if (selection.Count > 0)
				builder.Add(ViewControl.Export);
			return builder.ToImmutableHashSet();
		}

		void Reset()
		{
			viewItems = ImmutableList<ViewItem>.Empty;
			if (currentDialog != null)
			{
				currentDialog.TrySetResult(null);
				currentDialog = null;
				changeNotification.Post();
			}
		}

		class ViewItem : IViewItem
		{
			public IUserDefinedSearch Search { get; private set; }
			public bool Selected { get; private set; }

			public ViewItem(IUserDefinedSearch search, bool selected)
			{
				this.Search = search;
				this.Selected = selected;
			}

			string IListItem.Key => Search.Name;
			bool IListItem.IsSelected => Selected;
			public override string ToString() => Search.Name;
		}
	};
};