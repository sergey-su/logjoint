using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public class Presenter: IPresenter, IViewEvents
	{
		#region Public interface

		public Presenter(IView view, IPresentersFacade navHandler)
		{
			this.view = view;
			this.navHandler = navHandler;
		}

		void IPresenter.UpdateOpenWindow()
		{
			if (currentWindow != null)
				currentWindow._UpdateView();
		}

		void IPresenter.ShowWindow(ILogSource forSource)
		{
			currentWindow = view._CreateWindow(forSource, navHandler);
			try
			{
				currentWindow.ShowDialog();
			}
			finally
			{
				currentWindow = null;
			}
		}

		#endregion

		#region Implementation

		readonly IView view;
		readonly IPresentersFacade navHandler;
		IWindow currentWindow;

		#endregion
	};
};