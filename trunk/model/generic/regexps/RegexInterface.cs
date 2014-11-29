using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.RegularExpressions
{
	[Flags]
	public enum ReOptions
	{
		None = 0,
		Singleline = 1,
		Multiline = 2,
		RightToLeft = 4,
		IgnoreCase = 8,
		Timeboxed = 16
	};

	public struct Group
	{
		public int Index;
		public int Length;
		public Group(int idx, int len)
		{
			Index = idx;
			Length = len;
		}
	};

	public interface IMatch
	{
		IRegex OwnerRegex { get; }

		bool Success { get; }
		int Index { get; }
		int Length { get; }
		Group[] Groups { get; }
		
		void CopyFrom(IMatch src);
	};

	public interface IRegex
	{
		IRegexFactory Factory { get; }
		ReOptions Options { get; }
		string Pattern { get; }
		IEnumerable<string> GetGroupNames();
		bool Match(string str, int beginning, int length, ref IMatch match);
		bool Match(string str, int startat, ref IMatch match);
		bool Match(StringSlice str, int startat, ref IMatch match);
		IMatch CreateEmptyMatch();
	};

	public interface IRegexFactory
	{
		IRegex Create(string pattern, ReOptions options);
	};

	public static class RegexFactory
	{
		public static readonly IRegexFactory Instance =
#if !SILVERLIGHT
			//FCLRegexFactory.Instance;
			LJRegexFactory.Instance;
#else
			FCLRegexFactory.Instance;
#endif			
	};
}
