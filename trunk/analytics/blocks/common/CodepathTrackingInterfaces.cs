using System;

namespace LogJoint.Analytics
{
	public interface ICodepathTracker
	{
		void RegisterUsage(int codePathId);
	}

	/// <summary>
	/// Mark a program item with this attribute to tell telemetry analizer that
	/// code path is known to be obsolete and appropriate action is taken 
	/// (new replacement codepath is imeplemented for example).
	/// </summary>
	public class ObsoleteCodepathAttribute : Attribute
	{
	};

	/// <summary>
	/// Mark a program item with this attribute to tell telemetry analizer that
	/// code path is rare and therefore its small hit rate should not be a warning.
	/// </summary>
	public class RareCodepathAttribute : Attribute
	{
	};
}
