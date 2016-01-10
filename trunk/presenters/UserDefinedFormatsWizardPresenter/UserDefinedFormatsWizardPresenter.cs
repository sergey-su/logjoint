using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class Presenter : IPresenter
	{
		// note: it's stub implemenation.
		// doing proper presenter/view separation of existing WinForms wizrad requires lots of work.

		Action showDialog;

		public Presenter(Action showDialog)
		{
			this.showDialog = showDialog;
		}

		void IPresenter.ShowDialog()
		{
			showDialog();
		}
	};
};