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
		TL.IPresenter CreateTagsList(TL.IView view);
		TN.IPresenter CreateToastNotifications(TN.IView view);
		TN.IToastNotificationItem CreateCorrelatorToastNotificationItem();
		TN.IToastNotificationItem CreateUnprocessedLogsToastNotification(string postprocessorId);
	};

	public class PresentationObjectsFactory: IPresentationObjectsFactory
	{
		private readonly IPostprocessorsManager ppm;
		private readonly ILogSourcesManager lsm;
		private readonly IChangeNotification changeNotification;

		public PresentationObjectsFactory(
			IPostprocessorsManager ppm,
			ILogSourcesManager lsm,
			IChangeNotification changeNotification
		)
		{
			this.ppm = ppm;
			this.lsm = lsm;
			this.changeNotification = changeNotification;
		}

		TN.IToastNotificationItem IPresentationObjectsFactory.CreateCorrelatorToastNotificationItem ()
		{
			return new CorrelatorToastNotification(ppm, lsm);
		}

		TN.IToastNotificationItem IPresentationObjectsFactory.CreateUnprocessedLogsToastNotification (string postprocessorId)
		{
			return new UnprocessedLogsToastNotification(ppm, lsm, postprocessorId);
		}

		TL.IPresenter IPresentationObjectsFactory.CreateTagsList(TL.IView view)
		{
			return new TL.TagsListPresenter(view);
		}

		QS.IPresenter IPresentationObjectsFactory.CreateQuickSearch(QS.IView view)
		{
			return new QS.Presenter(view);
		}

		TN.IPresenter IPresentationObjectsFactory.CreateToastNotifications(TN.IView view)
		{
			return new TN.Presenter(view, changeNotification);
		}
	};
}
