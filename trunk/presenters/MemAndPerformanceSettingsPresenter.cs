using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.Settings.MemoryAndPerformance
{
	public interface IView
	{
		void SetPresenter(Presenter presenter);
	};

	public class Presenter
	{
		#region Public interface

		public interface ICallback
		{
		};

		public Presenter(Model model, IView view, ICallback callback)
		{
			this.model = model;
			this.view = view;
			this.callback = callback;
		}

		public void UpdateView()
		{
			//InternalUpdate();
		}

		#endregion

		#region Implementation

		readonly Model model;
		readonly IView view;
		readonly ICallback callback;

		#endregion
	};
};