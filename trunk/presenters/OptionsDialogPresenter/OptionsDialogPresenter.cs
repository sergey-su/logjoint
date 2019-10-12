using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.Options.Dialog
{
	public class Presenter : IPresenter, IDialogViewModel
	{
		public Presenter(
			IView view,
			Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory,
			Func<Appearance.IView, Appearance.IPresenter> appearancePresenterFactory,
			Func<UpdatesAndFeedback.IView, UpdatesAndFeedback.IPresenter> updatesAndFeedbackPresenterFactory,
			Func<Plugins.IView, Plugins.IPresenter> pluginsPresenterFactory)
		{
			this.view = view;
			this.memAndPerformancePagePresenterFactory = memAndPerformancePagePresenterFactory;
			this.appearancePresenterFactory = appearancePresenterFactory;
			this.updatesAndFeedbackPresenterFactory = updatesAndFeedbackPresenterFactory;
			this.pluginsPresenterFactory = pluginsPresenterFactory;
		}

		PageId IPresenter.VisiblePages => GetVisiblePages();

		void IPresenter.ShowDialog(PageId? initiallySelectedPage)
		{
			if (initiallySelectedPage != null &&
				(GetVisiblePages() & initiallySelectedPage.Value) == 0)
			{
				return;
			}
			using (var dialog = view.CreateDialog())
			{
				currentDialog = dialog;
				if (dialog.MemAndPerformancePage != null)
					memAndPerformancePagePresenter = memAndPerformancePagePresenterFactory(dialog.MemAndPerformancePage);
				if (dialog.ApperancePage != null)
					appearancePresenter = appearancePresenterFactory(dialog.ApperancePage);
				if (dialog.UpdatesAndFeedbackPage != null)
					updatesAndFeedbackPresenter = updatesAndFeedbackPresenterFactory(dialog.UpdatesAndFeedbackPage);
				if (dialog.PluginsPage != null)
					pluginPresenter = pluginsPresenterFactory(dialog.PluginsPage);
				currentDialog.SetViewModel(this);
				currentDialog.Show(initiallySelectedPage);
				currentDialog = null;
			}
		}

		void IDialogViewModel.OnOkPressed()
		{
			if (memAndPerformancePagePresenter?.Apply() == false)
				return;
			if (appearancePresenter?.Apply() == false)
				return;
			if (pluginPresenter?.Apply() == false)
				return;
			currentDialog.Hide();
			DisposePages();
		}

		void IDialogViewModel.OnCancelPressed()
		{
			currentDialog.Hide();
			DisposePages();
		}

		PageId IDialogViewModel.VisiblePages => GetVisiblePages();

		#region Implementation

		void DisposePages()
		{
			appearancePresenter?.Dispose();
			pluginPresenter?.Dispose();
		}

		PageId GetVisiblePages()
		{
			PageId result = PageId.Appearance | PageId.MemAndPerformance;
			if (updatesAndFeedbackPresenter?.IsAvailable == true)
				result |= PageId.UpdatesAndFeedback;
			if (pluginPresenter?.IsAvailable == true)
				result |= PageId.Plugins;
			return result;
		}

		readonly IView view;
		readonly Func<MemAndPerformancePage.IView, MemAndPerformancePage.IPresenter> memAndPerformancePagePresenterFactory;
		readonly Func<Appearance.IView, Appearance.IPresenter> appearancePresenterFactory;
		readonly Func<UpdatesAndFeedback.IView, UpdatesAndFeedback.IPresenter> updatesAndFeedbackPresenterFactory;
		readonly Func<Plugins.IView, Plugins.IPresenter> pluginsPresenterFactory;

		IDialog currentDialog;
		MemAndPerformancePage.IPresenter memAndPerformancePagePresenter;
		Appearance.IPresenter appearancePresenter;
		UpdatesAndFeedback.IPresenter updatesAndFeedbackPresenter;
		Plugins.IPresenter pluginPresenter;

		#endregion
	};
};