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
		bool ShowTheDialog(IFilter forFilter, IEnumerable<ILogSource> allSources, bool IsHighlightDialog);
	};

	public interface IPresenter
	{
		bool ShowTheDialog(IFilter forFilter);
	};

	public class Presenter : IPresenter
	{
		public Presenter(ILogSourcesManager logSources, IFiltersList filtersList, IView view)
		{
			this.logSources = logSources;
			this.filtersList = filtersList;
			this.view = view;
		}

		bool IPresenter.ShowTheDialog(IFilter forFilter)
		{
			return view.ShowTheDialog(forFilter, logSources.Items, true);
		}

		#region Implementation

		readonly ILogSourcesManager logSources;
		readonly IFiltersList filtersList;
		readonly IView view;

		#endregion
	};
};