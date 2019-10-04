using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RENS = System.Text.RegularExpressions.LogJointVersion;

namespace LogJoint.RegularExpressions
{
	public class LJRegex : IRegex
	{
		public LJRegex(IRegexFactory factory, string pattern, ReOptions options)
		{
			this.factory = factory;
			this.options = options;
			this.pattern = pattern;

			var opts = RENS.RegexOptions.Compiled |
				RENS.RegexOptions.ExplicitCapture;
			if ((options & ReOptions.AllowPatternWhitespaces) == 0)
				opts |= RENS.RegexOptions.IgnorePatternWhitespace;
			if ((options & RegularExpressions.ReOptions.Multiline) != 0)
				opts |= RENS.RegexOptions.Multiline;
			if ((options & RegularExpressions.ReOptions.Singleline) != 0)
				opts |= RENS.RegexOptions.Singleline;
			if ((options & RegularExpressions.ReOptions.RightToLeft) != 0)
				opts |= RENS.RegexOptions.RightToLeft;
			if ((options & RegularExpressions.ReOptions.IgnoreCase) != 0)
				opts |= RENS.RegexOptions.IgnoreCase;
			if ((options & RegularExpressions.ReOptions.Timeboxed) != 0)
				opts |= RENS.RegexOptions.Timeboxed;

			this.impl = new RENS.Regex(pattern, opts);
			this.groupNames = impl.GetGroupNames().ToArray();
		}

		public IRegexFactory Factory
		{
			get { return factory; }
		}

		public ReOptions Options
		{
			get { return options; }
		}

		public string Pattern
		{
			get { return pattern; }
		}

		public IEnumerable<string> GetGroupNames()
		{
			return groupNames;
		}

		public bool Match(string str, int beginning, int length, ref IMatch outMatch)
		{
			var matchImpl = GetOfCreateOutMatchImpl(ref outMatch);
			var srcMatch = impl.Match(str, beginning, length, ref matchImpl.match);
			matchImpl.Reload(0);
			return srcMatch.Success;
		}

		public bool Match(string str, int startat, ref IMatch outMatch)
		{
			var matchImpl = GetOfCreateOutMatchImpl(ref outMatch);
			var srcMatch = impl.Match(str, startat, ref matchImpl.match);
			matchImpl.Reload(0);
			return srcMatch.Success;
		}

		public bool Match(StringSlice slice, int startFrom, ref IMatch outMatch)
		{
			var matchImpl = GetOfCreateOutMatchImpl(ref outMatch);
			RENS.Match srcMatch;
			if ((options & ReOptions.RightToLeft) == 0)
				srcMatch = impl.Match(slice.Buffer, slice.StartIndex + startFrom, slice.Length - startFrom, ref matchImpl.match);
			else
				srcMatch = impl.Match(slice.Buffer, slice.StartIndex, startFrom, ref matchImpl.match);
			matchImpl.Reload(-slice.StartIndex);
			return srcMatch.Success;
		}

		public IMatch CreateEmptyMatch()
		{
			var ret = new LJMatch(this);
			return ret;
		}

		public int GroupsCount
		{
			get { return groupNames.Length; }
		}

		LJMatch GetOfCreateOutMatchImpl(ref IMatch outMatch)
		{
			LJMatch matchImp;
			if (outMatch == null)
			{
				matchImp = new LJMatch(this);
				outMatch = matchImp;
			}
			else
			{
				matchImp = outMatch as LJMatch;
				if (matchImp == null)
					throw new ArgumentException("outMatch has invalid type");
			}
			return matchImp;
		}

		readonly IRegexFactory factory;
		readonly string pattern;
		readonly ReOptions options;
		readonly RENS.Regex impl;
		readonly string[] groupNames;
	}

	public class LJMatch : IMatch
	{
		public IRegex OwnerRegex 
		{
			get { return re; }
		}

		public bool Success
		{
			get { return success; }
		}

		public int Index
		{
			get { return index; }
		}

		public int Length
		{
			get { return length; }
		}

		public Group[] Groups
		{
			get { return groups; }
		}

		public void CopyFrom(IMatch srcMatch)
		{
			LJMatch srcImpl = srcMatch as LJMatch;
			if (srcImpl == null)
				throw new ArgumentException("source Match has invalid type", "srcMatch");
			this.success = srcImpl.success;
			this.index = srcImpl.index;
			this.length = srcImpl.length;
			srcImpl.groups.CopyTo(this.groups, 0);
		}

		internal LJMatch(LJRegex re)
		{
			this.re = re;
			this.groups = new Group[re.GroupsCount];
		}

		internal void Reload(int offset)
		{
			success = match.Success;
			index = match.Index + offset;
			length = match.Length;

			var srcGroups = match.Groups;
			for (int i = 0; i < srcGroups.Count; ++i)
			{
				var srcGroup = srcGroups[i];
				groups[i] = new Group(srcGroup.Index + offset, srcGroup.Length);
			}
		}

		readonly LJRegex re;
		
		bool success;
		int index;
		int length;
		readonly Group[] groups;

		internal RENS.Match match;
	};

	public class LJRegexFactory : IRegexFactory
	{
		public IRegex Create(string pattern, ReOptions options)
		{
			return new LJRegex(this, pattern, options);
		}

		public static readonly LJRegexFactory Instance = new LJRegexFactory();
	};
}
