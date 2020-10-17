using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.AlertPopup
{
	class Presenter: IAlertPopup, IViewModel
	{
		readonly IChangeNotification changeNotification;
		ViewState viewState;

		public Presenter(IChangeNotification changeNotification)
		{
			this.changeNotification = changeNotification;
		}

		AlertFlags IAlertPopup.ShowPopup(string caption, string text, AlertFlags flags) =>
			throw new NotImplementedException("sync popups not supported");

		Task<AlertFlags> IAlertPopup.ShowPopupAsync(string caption, string text, AlertFlags flags)
		{
			EnsureHidden();
			viewState = new ViewState()
			{
				caption = caption,
				text = text,
				flags = flags,
				taskSource = new TaskCompletionSource<AlertFlags>(),
			};
			changeNotification.Post();
			return viewState.taskSource.Task;
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		IViewState IViewModel.ViewState => viewState;
		void IViewModel.OnButtonClicked(AlertFlags button)
		{
			EnsureHidden(button);
		}
		void IViewModel.OnClickOutside()
		{
			EnsureHidden();
		}

		void EnsureHidden(AlertFlags result = AlertFlags.Cancel) // todo: what if cancel is not an option?
		{
			if (viewState == null)
				return;
			viewState.taskSource.SetResult(result); 
			viewState = null;
			changeNotification.Post();
		}

		class ViewState : IViewState
		{
			public string caption, text;
			public AlertFlags flags;
			public TaskCompletionSource<AlertFlags> taskSource;

			string IViewState.Caption => caption;
			string IViewState.Text => text;
			AlertFlags IViewState.Buttons => flags & AlertFlags.ButtonsMask;
			AlertFlags IViewState.Icon => flags & AlertFlags.IconsMask;
		};

	};
}