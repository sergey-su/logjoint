using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Reactive
{
	class Utils
	{
		// A helper function that applies the given action to the View. It waits for a View 
		// to be attached to the Presenter till the next event loop's iteration.
		// The algorithms is useful for presenters that connect to their views dynamically.
		// When a presenter needs to perform an action on its View, there might be no View 
		// attached to the Presenter at the moment. The View may be waiting in the change notification
		// queue to be rendered and attached to the Presenter.
		public static async void PerformViewAction<V>(Func<V> viewGetter, Action<V> action)
		{
			V view = viewGetter();
			if (view == null)
				await Task.Yield();
			view = viewGetter();
			if (view != null)
				action(view);
		}
	}
}
