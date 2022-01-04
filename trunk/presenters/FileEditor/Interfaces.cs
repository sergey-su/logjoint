
namespace LogJoint.UI.Presenters.FileEditor
{
	public interface IView
	{
	};

	public interface IPresenter
	{
		void ShowDialog(string file, bool readOnly);
	};

	public interface IViewModel
	{
		void SetView(IView view);
		IChangeNotification ChangeNotification { get; }

		bool IsVisible { get; }
		bool IsReadOnly { get; }
		string Caption { get; }
		string Contents { get; }
		bool IsSaveButtonVisible { get; }
		bool IsDownloadButtonVisible { get; }

		void OnClose();
		void OnSave();
		void OnDownload();
		void OnChange(string value);
	};
};