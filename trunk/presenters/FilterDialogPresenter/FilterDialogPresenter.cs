using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.FilterDialog
{
	public interface IView
	{
		// todo: all logic is in view now. move presentation logic to presenter.
		bool ShowTheFreakingDialog(Filter forFilter, IEnumerable<ILogSource> allSources, bool IsHighlightDialog);
	};

	public interface IPresenter
	{
		bool ShowTheDialog(Filter forFilter);
	};

	public class Presenter : IPresenter
	{
		public Presenter(IModel model, IFiltersList filtersList, IView view)
		{
			this.model = model;
			this.filtersList = filtersList;
			this.view = view;
		}

		bool IPresenter.ShowTheDialog(Filter forFilter)
		{
			return view.ShowTheFreakingDialog(forFilter, model.SourcesManager.Items, model.HighlightFilters == filtersList);
		}

		#region Implementation

		readonly IModel model;
		readonly IFiltersList filtersList;
		readonly IView view;

		#endregion
	};
};