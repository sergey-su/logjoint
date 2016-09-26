using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.Options.Dialog
{
	public class Presenter : IPresenter, IPresenterEvents
	{
		public Presenter(
			IView view,
			Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory,
			Func<Appearance.IView, Appearance.IPresenter> appearancePresenterFactory,
			Func<UpdatesAndFeedback.IView, UpdatesAndFeedback.IPresenter> updatesAndFeedbackPresenterFactory)
		{
			this.view = view;
			this.memAndPerformancePagePresenterFactory = memAndPerformancePagePresenterFactory;
			this.appearancePresenterFactory = appearancePresenterFactory;
			this.updatesAndFeedbackPresenterFactory = updatesAndFeedbackPresenterFactory;

			view.SetPresenter(this);
		}

		void IPresenter.ShowDialog()
		{
			using (var dialog = view.CreateDialog())
			{
				currentDialog = dialog;
				memAndPerformancePagePresenter = memAndPerformancePagePresenterFactory(dialog.MemAndPerformancePage);
				appearancePresenter = appearancePresenterFactory(dialog.ApperancePage);
				updatesAndFeedbackPresenter = updatesAndFeedbackPresenterFactory(dialog.UpdatesAndFeedbackPage);
				currentDialog.SetUpdatesAndFeedbackPageVisibility(updatesAndFeedbackPresenter.IsAvailable);
				currentDialog.Show();
				currentDialog = null;
			}
		}

		void IPresenterEvents.OnOkPressed()
		{
			if (!memAndPerformancePagePresenter.Apply())
				return;
			if (!appearancePresenter.Apply())
				return;
			currentDialog.Hide();
		}

		void IPresenterEvents.OnCancelPressed()
		{
			currentDialog.Hide();
		}

		#region Implementation

		readonly IView view;
		readonly Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory;
		readonly Func<Appearance.IView, Appearance.IPresenter> appearancePresenterFactory;
		readonly Func<UpdatesAndFeedback.IView, UpdatesAndFeedback.IPresenter> updatesAndFeedbackPresenterFactory;
		
		IDialog currentDialog;
		MemAndPerformancePage.IPresenter memAndPerformancePagePresenter;
		Appearance.IPresenter appearancePresenter;
		UpdatesAndFeedback.IPresenter updatesAndFeedbackPresenter;

		#endregion
	};
};