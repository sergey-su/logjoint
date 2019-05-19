using System.Collections.Generic;

namespace LogJoint.Analytics
{
	public interface ITagged
	{
		HashSet<string> Tags { get; set; }
	};
}
