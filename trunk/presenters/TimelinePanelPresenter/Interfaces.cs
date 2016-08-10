using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.TimelinePanel
{
	public interface IPresenter
	{
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
	};


	public interface IViewEvents
	{
		void OnZoomToolButtonClicked(int delta);
		void OnZoomToViewAllToolButtonClicked();
		void OnScrollToolButtonClicked(int delta);
	};
};