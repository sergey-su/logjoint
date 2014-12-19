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

	public interface IView // todo: Bad view interface. Split functionality between view and presenter.
	{
		void SetPresenter(IViewEvents presenter);
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


	public interface IViewEvents
	{
		void OnNavigate(TimeNavigateEventArgs evt);
		void OnRangeChanged();
		void OnBeginTimeRangeDrag();
		void OnEndTimeRangeDrag();



		// todo: below is a bad interface between view and presenter. refactor it.
		RulerIntervals? FindRulerIntervals(TimeSpan minSpan);
		IEnumerable<RulerMark> GenerateRulerMarks(RulerIntervals intervals, DateRange range);

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

	public enum DateComponent
	{
		None,
		Year,
		Month,
		Day,
		Hour,
		Minute,
		Seconds,
		Milliseconds
	};

	public struct RulerInterval
	{
		public readonly TimeSpan Duration;
		public readonly DateComponent Component;
		public readonly int NonUniformComponentCount;
		public readonly bool IsHiddenWhenMajor;

		public RulerInterval(TimeSpan dur, int nonUniformComponentCount, DateComponent comp, bool isHiddenWhenMajor = false)
		{
			Duration = dur;
			Component = comp;
			NonUniformComponentCount = nonUniformComponentCount;
			IsHiddenWhenMajor = isHiddenWhenMajor;
		}
	};

	public struct RulerIntervals
	{
		public readonly RulerInterval Major, Minor;
		public RulerIntervals(RulerInterval major, RulerInterval minor)
		{
			Major = major;
			Minor = minor;
		}
	};

	public struct RulerMark
	{
		public readonly DateTime Time;
		public readonly bool IsMajor;
		public readonly DateComponent Component;

		public RulerMark(DateTime d, bool isMajor, DateComponent comp)
		{
			Time = d;
			IsMajor = isMajor;
			Component = comp;
		}
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