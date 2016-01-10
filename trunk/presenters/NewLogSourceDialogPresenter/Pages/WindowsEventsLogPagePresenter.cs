using System;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog
{
	public interface IView
	{
		object PageView { get; }
		void SetEventsHandler(IViewEvents eventsHandler);
		void SetSelectedLogText(string value);
	};

	public interface IViewEvents
	{
		void OnIdentitySelected(WindowsEventLog.EventLogIdentity identity);
	};

	public class Presenter : IPagePresenter, IViewEvents
	{
		readonly IView view;
		readonly IModel model;
		WindowsEventLog.EventLogIdentity currentIdentity;
		WindowsEventLog.Factory factory;

		public Presenter(IView view, ILogProviderFactory factory, IModel model)
		{
			this.view = view;
			this.factory = (WindowsEventLog.Factory)factory;
			this.model = model;
			view.SetEventsHandler(this);
		}

		void IPagePresenter.Apply()
		{
			if (currentIdentity == null)
				return;
			IConnectionParams connectParams = factory.CreateParamsFromIdentity(currentIdentity);
			model.CreateLogSource(factory, connectParams);
			SetCurrentIdentity(null);
		}

		void IPagePresenter.Activate()
		{
		}

		void IPagePresenter.Deactivate()
		{
		}

		object IPagePresenter.View
		{
			get { return view.PageView; }
		}

		void IViewEvents.OnIdentitySelected(WindowsEventLog.EventLogIdentity identity)
		{
			SetCurrentIdentity(identity);
		}

		void IDisposable.Dispose()
		{
		}

		void SetCurrentIdentity(WindowsEventLog.EventLogIdentity id)
		{
			currentIdentity = id;
			view.SetSelectedLogText((id != null) ? id.ToUserFriendlyString() : "");
		}
	};
};