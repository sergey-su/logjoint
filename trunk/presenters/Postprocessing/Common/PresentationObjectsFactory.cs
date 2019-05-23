using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TN = LogJoint.UI.Presenters.ToastNotificationPresenter;
using TL = LogJoint.UI.Presenters.TagsList;
using QS = LogJoint.UI.Presenters.QuickSearchTextBox;
using LogJoint.Postprocessing;

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
		private readonly IPostprocessorsManager ppm;
		private readonly ILogSourcesManager lsm;
		private readonly IAlertPopup alerts;

		public PresentationObjectsFactory(
			IPostprocessorsManager ppm,
			ILogSourcesManager lsm,
			IChangeNotification changeNotification,
			IAlertPopup alerts
		)
		{
			this.ppm = ppm;
			this.lsm = lsm;
			this.alerts = alerts;
		}

		TN.IToastNotificationItem IPresentationObjectsFactory.CreateCorrelatorToastNotificationItem ()
		{
			return new CorrelatorToastNotification(ppm, lsm);
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
