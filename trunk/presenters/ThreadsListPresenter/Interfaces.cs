using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.ThreadsList
{
	public interface IPresenter
	{
		void Select(IThread thread);
	};


	public interface IViewItem
	{
		IThread Thread { get; }
		void SetSubItemText(int subItemIdx, string text);
		void SetSubItemBookmark(int subItemIdx, IBookmark bmk);
		string Text { get; set; }
		bool Selected { get; set; }
	};

	public interface IView
	{
		void SetPresenter(Presenter presenter);
		void BeginBulkUpdate();
		void EndBulkUpdate();
		IEnumerable<IViewItem> Items { get; }
		void RemoveItem(IViewItem item);
		IViewItem Add(IThread thread);
		IViewItem TopItem { get; set; }
		void SortItems();
		void UpdateFocusedThreadView();
		void SetThreadsDiscoveryState(bool inProgress);
	};

	public interface IViewEvents
	{
		void OnAddNewLogButtonClicked();
		void OnDeleteSelectedLogSourcesButtonClicked();
		void OnDeleteAllLogSourcesButtonClicked();
		void OnMRUButtonClicked();
		void OnMRUMenuItemClicked(string itemId);
		void OnTrackingChangesCheckBoxChecked(bool value);
	};
};