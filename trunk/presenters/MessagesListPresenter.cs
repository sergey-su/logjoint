using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.MessagesList
{
	public interface IView
	{
	};

	public class Presenter
	{
		#region Public interface

		public interface ICallback
		{
			void ShowMessageProperties(MessageBase msg);
		};

		public Presenter(IModel model, IView view, ICallback callback)
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
		
		readonly IModel model;
		readonly IView view;
		readonly ICallback callback;
		
		#endregion
	};
};