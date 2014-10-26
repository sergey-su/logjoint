using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.SearchPanel
{
	public class Presenter : IPresenter, IPresenterEvents
	{
		public Presenter(
			Model model,
			IView view)
		{
			this.model = model;
			this.view = view;
		}


		#region Implementation

		readonly Model model;
		readonly IView view;

		#endregion
	};
};