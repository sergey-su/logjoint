using LogJoint.UI.Presenters.Reactive;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public interface IPresenter
	{
		Task<IUserDefinedSearch> Open();
	};

	public interface IViewItem: IListItem
	{
	};

	public enum ViewControl
	{
		AddButton,
		DeleteButton,
		EditButton,
		Export,
		Import,
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		bool IsVisible { get; }
		string CloseButtonText { get; }
		IReadOnlySet<ViewControl> EnabledControls { get; }
		IReadOnlyList<IViewItem> Items { get; }
		void OnCloseClicked();
		void OnCancelled();
		void OnAddClicked();
		void OnDeleteClicked();
		void OnEditClicked();
		void OnSelect(IEnumerable<IViewItem> requestedSelection);
		void OnExportClicked();
		void OnImportClicked();
	};
};