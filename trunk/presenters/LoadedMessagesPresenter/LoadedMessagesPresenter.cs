using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class Presenter: IPresenter
	{
		readonly Model model;
		readonly IView view;
		LogViewer.Presenter messagesPresenter;

		public Presenter(
			Model model,
			IView view,
			Presenters.LogViewer.Presenter.ICallback messagePresenterCallback // todo: have own callback interface
		)
		{
			this.model = model;
			this.view = view;
			this.messagesPresenter = new Presenters.LogViewer.Presenter(
				new PresentationModel(model), view.MessagesView, messagePresenterCallback);
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.Presenter.PreferredDblClickAction.SelectWord;
			this.UpdateRawViewButton();
			this.UpdateColoringControls();
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();
		}

		bool IPresenter.RawViewAllowed
		{
			get { return messagesPresenter.RawViewAllowed; }
			set { messagesPresenter.RawViewAllowed = value; }
		}

		void IPresenter.UpdateView()
		{
			messagesPresenter.UpdateView();
		}

		void IPresenter.ToggleBookmark()
		{
			var msg = messagesPresenter.Selection.Message;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		void IPresenter.ToggleRawView()
		{
			messagesPresenter.ShowRawMessages = messagesPresenter.RawViewAllowed && !messagesPresenter.ShowRawMessages;
		}

		void IPresenter.ColoringButtonClicked(LogViewer.ColoringMode mode)
		{
			messagesPresenter.Coloring = mode;
			UpdateColoringControls();
		}

		LogViewer.Presenter IPresenter.LogViewerPresenter
		{
			get { return messagesPresenter; }
		}

		void UpdateRawViewButton()
		{
			view.SetRawViewButtonState(messagesPresenter.RawViewAllowed, messagesPresenter.ShowRawMessages);
		}

		void UpdateColoringControls()
		{
			var coloring = messagesPresenter.Coloring;
			view.SetColoringButtonsState(
				coloring == LogViewer.ColoringMode.None,
				coloring == LogViewer.ColoringMode.Sources,
				coloring == LogViewer.ColoringMode.Threads
			);
		}
	};
};