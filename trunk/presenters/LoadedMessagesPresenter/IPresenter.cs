using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IPresenter
	{
		Presenters.LogViewer.IPresenterInternal LogViewerPresenter { get; }
		Task<Dictionary<ILogSource, long>> GetCurrentLogPositions(CancellationToken cancellation);
		event EventHandler OnResizingStarted;
		event EventHandler<ResizingEventArgs> OnResizing;
		event EventHandler OnResizingFinished;
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		ViewState ViewState { get; }

		void OnToggleRawView();
		void OnColoringButtonClicked(int mode);
		void OnToggleBookmark();
		void OnResizingStarted();
		void OnResizingFinished();
		void OnResizing(int delta);
		void OnToggleViewTail();
	};

	public class ViewState
	{
		public (bool Visible, string Tooltip) ToggleBookmark { get; internal set; }
		public (bool Visible, bool Checked, string Tooltip) RawViewButton { get; internal set; }
		public (bool Visible, IReadOnlyList<(string Text, string Tooltip)> Options, int Selected) Coloring { get; internal set; }
		public (bool Visible, string Tooltip) NavigationProgressIndicator { get; internal set; }
		public (bool Visible, bool Checked, string Tooltip) ViewTailButton { get; internal set; }
	};
};