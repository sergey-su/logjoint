using TN = LogJoint.UI.Presenters.ToastNotificationPresenter;
using TL = LogJoint.UI.Presenters.TagsList;
using QS = LogJoint.UI.Presenters.QuickSearchTextBox;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
	public interface IPresentationObjectsFactory
	{
		QS.IPresenter CreateQuickSearch(QS.IView view);
		TL.IPresenter CreateTagsList(IPostprocessorTags model, TL.IView view, IChangeNotification changeNotification);
		TN.IPresenter CreateToastNotifications(TN.IView view, IChangeNotification changeNotification);
		TN.IToastNotificationItem CreateCorrelatorToastNotificationItem();
		TN.IToastNotificationItem CreateUnprocessedLogsToastNotification(PostprocessorKind postprocessorKind);
	};

	public class PresentationObjectsFactory: IPresentationObjectsFactory
	{
		private readonly IManagerInternal ppm;
		private readonly ILogSourcesManager lsm;
		private readonly IAlertPopup alerts;
		private readonly ICorrelationManager correlationManager;

		public PresentationObjectsFactory(
			IManagerInternal ppm,
			ILogSourcesManager lsm,
			IChangeNotification changeNotification,
			IAlertPopup alerts,
			ICorrelationManager correlationManager
		)
		{
			this.ppm = ppm;
			this.lsm = lsm;
			this.alerts = alerts;
			this.correlationManager = correlationManager;
		}

		TN.IToastNotificationItem IPresentationObjectsFactory.CreateCorrelatorToastNotificationItem ()
		{
			return new CorrelatorToastNotification(ppm, lsm, correlationManager);
		}

		TN.IToastNotificationItem IPresentationObjectsFactory.CreateUnprocessedLogsToastNotification (PostprocessorKind postprocessorKind)
		{
			return new UnprocessedLogsToastNotification(ppm, lsm, postprocessorKind);
		}

		TL.IPresenter IPresentationObjectsFactory.CreateTagsList(IPostprocessorTags model, TL.IView view, IChangeNotification changeNotification)
		{
			return new TL.TagsListPresenter(model, view, changeNotification, alerts);
		}

		QS.IPresenter IPresentationObjectsFactory.CreateQuickSearch(QS.IView view)
		{
			return new QS.Presenter(view);
		}

		TN.IPresenter IPresentationObjectsFactory.CreateToastNotifications(TN.IView view, IChangeNotification changeNotification)
		{
			return new TN.Presenter(view, changeNotification);
		}
	};
}
