using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
	public interface IView
	{
		// todo: all logic is in the view now. move presentation logic to presenter.
		IDialog CreateDialog(IModel model, IFactoryUICallback callback, Preprocessing.IPreprocessingUserRequests userRequests);
	};

	public interface IDialog
	{
		void Show();
	};

	public interface IPresenter
	{
		void ShowTheDialog();
	};

	public class Presenter : IPresenter
	{
		public Presenter(IModel model, IFactoryUICallback factoryUICallback, IView view, Preprocessing.IPreprocessingUserRequests preprocessingUserRequests)
		{
			this.model = model;
			this.factoryUICallback = factoryUICallback;
			this.view = view;
			this.preprocessingUserRequests = preprocessingUserRequests;
		}

		void IPresenter.ShowTheDialog()
		{
			if (dialog == null)
				dialog = view.CreateDialog(model, factoryUICallback, preprocessingUserRequests);
			dialog.Show();
		}

		#region Implementation

		readonly IModel model;
		readonly IFactoryUICallback factoryUICallback;
		readonly IView view;
		readonly Preprocessing.IPreprocessingUserRequests preprocessingUserRequests;
		IDialog dialog;

		#endregion
	};
};