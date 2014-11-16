using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.Timeline
{
	public interface IPresenter
	{
		event EventHandler<EventArgs> RangeChanged;
		void UpdateView();
		void Zoom(int delta);
		void Scroll(int delta);
		void ZoomToViewAll();
		void TrySwitchOnViewTailMode();
		void TrySwitchOffViewTailMode();
		bool AreMillisecondsVisible { get; }
	};

	public interface IView // todo: Bad view interfece. Split functionality between view and presenter.
	{
		void SetPresenter(IPresenterEvents presenter);
		void Invalidate();
		void Update();
		void Zoom(int delta);
		void Scroll(int delta);
		void ZoomToViewAll();
		void TrySwitchOnViewTailMode();
		void TrySwitchOffViewTailMode();
		bool AreMillisecondsVisible { get; }
		DateRange TimeRange { get; }
	};


	public interface IPresenterEvents
	{
		void OnNavigate(TimeNavigateEventArgs evt);
		void OnRangeChanged();
		void OnBeginTimeRangeDrag();
		void OnEndTimeRangeDrag();


		// todo: below is a bad interface between view and presenter. refactor it.
		IEnumerable<ILogSource> Sources { get; }
		int SourcesCount { get; }
		DateTime? CurrentViewTime { get; }
		ILogSource CurrentSource { get; }
		StatusReports.IReport CreateNewStatusReport();
		IEnumerable<IBookmark> Bookmarks { get; }
		bool FocusRectIsRequired { get; }
		bool IsInViewTailMode { get; }
		bool IsBusy { get; }
	};

	public class TimeNavigateEventArgs : EventArgs
	{
		public TimeNavigateEventArgs(DateTime date, NavigateFlag flags, ILogSource source)
		{
			this.date = date;
			this.flags = flags;
			this.source = source;
		}
		public DateTime Date { get { return date; } }
		public NavigateFlag Flags { get { return flags; } }
		public ILogSource Source { get { return source; } }

		DateTime date;
		NavigateFlag flags;
		ILogSource source;
	};
};