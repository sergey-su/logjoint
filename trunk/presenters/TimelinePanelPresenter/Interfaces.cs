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
		void SetPresenter(IPresenterEvents presenter);
		void SetViewTailModeToolButtonState(bool buttonChecked);
	};


	public interface IPresenterEvents
	{
		void OnZoomToolButtonClicked(int delta);
		void OnZoomToViewAllToolButtonClicked();
		void OnScrollToolButtonClicked(int delta);
		void OnViewTailModeToolButtonClicked(bool viewTailModeRequested);
	};
};