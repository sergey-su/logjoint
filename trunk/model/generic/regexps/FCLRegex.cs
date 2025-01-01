using System;
using System.Collections.Generic;
using System.Linq;
using RENS = System.Text.RegularExpressions;

namespace LogJoint.RegularExpressions
{
    public class FCLRegex : IRegex
    {
        public FCLRegex(IRegexFactory factory, string pattern, ReOptions options, RENS.Regex precompiledRegex)
        {
            this.factory = factory;
            this.options = options;
            this.pattern = pattern;

            var opts = GetOptions(options);

            if (precompiledRegex != null)
            {
                if (RemoveCompiled(precompiledRegex.Options) != RemoveCompiled(opts))
                    throw new ArgumentException($"The options of precompiled regex does not match the expected one: " +
                        $"{precompiledRegex.Options} vs expected {opts}", nameof(precompiledRegex));
                if (precompiledRegex.ToString() != pattern)
                    throw new ArgumentException($"The pattern of precompiled regex does not match the expected one: " +
                        $"{precompiledRegex} vs expected {pattern}", nameof(precompiledRegex));
                if (precompiledRegex.MatchTimeout != RENS.Regex.InfiniteMatchTimeout)
                    throw new ArgumentException($"Precompiled regex should not have timeout", nameof(precompiledRegex));

                if ((options & ReOptions.Timeboxed) == 0)
                    this.impl = precompiledRegex;
                else
                    this.impl = new RENS.Regex(pattern, opts, timboxedTimeout);
            }
            else
            {
                this.impl = new RENS.Regex(pattern, opts);
            }

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
            var matchImpl = GetOrCreateOutMatchImpl(ref outMatch);
            var srcMatch = impl.Match(str, beginning, length);
            matchImpl.Init(srcMatch, 0);
            if (!srcMatch.Success)
                return false;
            return true;
        }

        public bool Match(string str, int startat, ref IMatch outMatch)
        {
            var matchImpl = GetOrCreateOutMatchImpl(ref outMatch);
            var srcMatch = impl.Match(str, startat);
            matchImpl.Init(srcMatch, 0);
            if (!srcMatch.Success)
                return false;
            return true;
        }

        public bool Match(StringSlice slice, int startFrom, ref IMatch outMatch)
        {
            var matchImpl = GetOrCreateOutMatchImpl(ref outMatch);
            RENS.Match srcMatch;
            if ((options & ReOptions.RightToLeft) == 0)
                srcMatch = impl.Match(slice.Buffer, slice.StartIndex + startFrom, slice.Length - startFrom);
            else
                srcMatch = impl.Match(slice.Buffer, slice.StartIndex, startFrom);
            matchImpl.Init(srcMatch, -slice.StartIndex);
            if (!srcMatch.Success)
                return false;
            return true;
        }

        public IMatch CreateEmptyMatch()
        {
            var ret = new FCLMatch(this);
            ret.Init(RENS.Match.Empty, 0);
            return ret;
        }

        public int GroupsCount
        {
            get { return groupNames.Length; }
        }

        public IRegex ToTimeboxed()
        {
            if (impl.MatchTimeout != RENS.Regex.InfiniteMatchTimeout)
                return this;
            return new FCLRegex(factory, pattern, options | ReOptions.Timeboxed, null);
        }

        FCLMatch GetOrCreateOutMatchImpl(ref IMatch outMatch)
        {
            FCLMatch matchImp;
            if (outMatch == null)
            {
                matchImp = new FCLMatch(this);
                outMatch = matchImp;
            }
            else
            {
                matchImp = outMatch as FCLMatch;
                if (matchImp == null)
                    throw new ArgumentException("outMatch has invalid type");
            }
            return matchImp;
        }

        static RENS.RegexOptions GetOptions(ReOptions options)
        {
            var opts =
                RENS.RegexOptions.Compiled |
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
            return opts;
        }

        static RENS.RegexOptions RemoveCompiled(RENS.RegexOptions options) =>
            options & ~RENS.RegexOptions.Compiled;

        readonly IRegexFactory factory;
        readonly string pattern;
        readonly ReOptions options;
        readonly RENS.Regex impl;
        readonly string[] groupNames;
        static readonly TimeSpan timboxedTimeout = TimeSpan.FromMilliseconds(500);
    }

    public class FCLMatch : IMatch
    {
        internal FCLMatch(FCLRegex re)
        {
            this.re = re;
            this.groups = new Group[re.GroupsCount];
        }

        public IRegex OwnerRegex
        {
            get { return re; }
        }

        public bool Success
        {
            get { return match.Success; }
        }

        public int Index
        {
            get { return match.Index + offset; }
        }

        public int Length
        {
            get { return match.Length; }
        }

        public Group[] Groups
        {
            get { return groups; }
        }

        public void CopyFrom(IMatch srcMatch)
        {
            FCLMatch srcImpl = srcMatch as FCLMatch;
            if (srcImpl == null)
                throw new ArgumentException("srcMatch has invalid type", nameof(srcMatch));
            this.match = srcImpl.match;
            srcImpl.groups.CopyTo(this.groups, 0);
        }

        internal void Init(RENS.Match src, int offset)
        {
            this.match = src;
            var srcGroups = match.Groups;
            this.offset = offset;
            for (int i = 0; i < srcGroups.Count; ++i)
            {
                var srcGroup = srcGroups[i];
                groups[i] = new Group(srcGroup.Index + offset, srcGroup.Length);
            }
        }

        readonly FCLRegex re;
        readonly Group[] groups;

        RENS.Match match;
        int offset;
    };

    public class FCLRegexFactory : IRegexFactory
    {
        public IRegex Create(string pattern, ReOptions options, System.Text.RegularExpressions.Regex precompiledRegex)
        {
            return new FCLRegex(this, pattern, options, precompiledRegex);
        }

        public static readonly FCLRegexFactory Instance = new FCLRegexFactory();
    };
}
