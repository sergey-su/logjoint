using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public enum PreferredViewMode
	{
		Normal,
		Raw
	};

	public interface IFormatViewOptions
	{
		PreferredViewMode PreferredView { get; }
		bool RawViewAllowed { get; }
		bool AlwaysShowMilliseconds { get; }
		int? WrapLineLength { get; }
	}
}
