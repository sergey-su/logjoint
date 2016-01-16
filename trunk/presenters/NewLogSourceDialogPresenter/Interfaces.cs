using System;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
	public interface IPresenter
	{
		void ShowTheDialog(string selectedPageName = null);
		IPagePresentersRegistry PagesRegistry { get; }
		string FotmatDetectorPageName { get; }
	};

	public interface IPagePresenter : IDisposable
	{
		void Apply();
		void Activate();
		void Deactivate();
		object View { get; }
	};

	public interface IPagePresentersRegistry
	{
		IPagePresenter CreatePagePresenter(ILogProviderFactory factory);
		
		void RegisterPagePresenterFactory(string key, Func<ILogProviderFactory, IPagePresenter> factory);
	};


	public interface IDialogViewEvents
	{
		void OnSelectedIndexChanged();
		void OnOKButtonClicked();
		void OnCancelButtonClicked();
		void OnApplyButtonClicked();
		void OnManageFormatsButtonClicked();
	};

	public interface IView
	{
		IDialogView CreateDialog(IDialogViewEvents eventsHandler);
	};

	public interface IViewListItem
	{
	};

	public interface IDialogView
	{
		void ShowModal();
		void EndModal();
		void SetList(IViewListItem[] items, int selectedIndex);
		IViewListItem GetItem(int idx);
		int SelectedIndex { get; }
		void DetachPageView(object view);
		void AttachPageView(object view);
		void SetFormatControls(string nameLabelValue, string descriptionLabelValue);
	};
};