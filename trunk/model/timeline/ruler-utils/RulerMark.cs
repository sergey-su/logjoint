using System;

namespace LogJoint
{
	/// <summary>
	/// A line on logjoint's timeline. The line represents some time interval boundary.
	/// For instance, a minute's boundary.
	/// </summary>
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

}
