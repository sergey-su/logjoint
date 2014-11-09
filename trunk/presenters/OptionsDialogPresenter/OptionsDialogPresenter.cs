using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.Options.Dialog
{
	public class Presenter : IPresenter, IPresenterEvents
	{
		public Presenter(
			IModel model,
			IView view,
			Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory)
		{
			this.model = model;
			this.view = view;
			this.memAndPerformancePagePresenterFactory = memAndPerformancePagePresenterFactory;

			view.SetPresenter(this);
		}

		void IPresenter.ShowDialog()
		{
			using (var dialog = view.CreateDialog())
			{
				currentDialog = dialog;
				memAndPerformancePagePresenter = memAndPerformancePagePresenterFactory(dialog.MemAndPerformancePage);
				currentDialog.Show();
				currentDialog = null;
			}
		}

		void IPresenterEvents.OnOkPressed()
		{
			if (!memAndPerformancePagePresenter.Apply())
				return;
			currentDialog.Hide();
		}

		void IPresenterEvents.OnCancelPressed()
		{
			currentDialog.Hide();
		}

		#region Implementation

		readonly IModel model;
		readonly IView view;
		readonly Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory;
		
		IDialog currentDialog;
		MemAndPerformancePage.IPresenter memAndPerformancePagePresenter;

		#endregion
	};
};