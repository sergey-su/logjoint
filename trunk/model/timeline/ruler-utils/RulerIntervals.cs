
namespace LogJoint
{
	/// <summary>
	/// Logjoint's timeline ruler depicts lines that represent boundaries of time intervals.
	/// Timeline may show at most two intervals at a time: major and minor intervals.
	/// For instance, major intervals may represenet seconds and monor intervals - hudreds of milliseconds.
	/// Which pair of intervals to show depends on time scale.
	/// This struct represents a pair of intervals detected by <seealso cref="RulerUtils.FindRulerIntervals()"/>
	/// </summary>
	public struct RulerIntervals
	{
		public readonly RulerInterval Major, Minor;
		public RulerIntervals(RulerInterval major, RulerInterval minor)
		{
			Major = major;
			Minor = minor;
		}
	};
}
