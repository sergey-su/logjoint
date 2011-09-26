using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SourcesList
{
	public interface IView
	{
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
		}

		#endregion

		#region Implementation
		
		readonly Model model;
		readonly IView view;
		readonly ICallback callback;
		
		#endregion
	};
};