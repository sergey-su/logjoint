using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;

namespace LogJoint
{
	public static class Search
	{
		public class PreprocessedOptions
		{
			internal Options options;
			internal IRegex re;
			internal MessageBase.MessageFlag typeMask;
			internal MessageBase.MessageFlag msgTypeMask;
		};

		public class TemplateException : Exception
		{
		};

		public struct Options
		{
			public string Template;
			public bool WholeWord;
			public bool Regexp;
			public IThread SearchWithinThisThread;
			public bool MatchCase;
			public bool ReverseSearch;
			public MessageBase.MessageFlag TypesToLookFor;
			public bool WrapAround;
			public long MessagePositionToStartSearchFrom;
			public PreprocessedOptions Preprocess()
			{
				PreprocessedOptions ret = new PreprocessedOptions() { 
					options = this,
					typeMask = MessageBase.MessageFlag.TypeMask & TypesToLookFor,
					msgTypeMask = MessageBase.MessageFlag.ContentTypeMask & TypesToLookFor
				};
				if (!string.IsNullOrEmpty(Template))
				{
					if (Regexp)
					{
						ReOptions reOpts = ReOptions.None;
						if (!MatchCase)
							reOpts |= ReOptions.IgnoreCase;
						if (ReverseSearch)
							reOpts |= ReOptions.RightToLeft;
						try
						{
							ret.re = RegexFactory.Instance.Create(Template, reOpts);
						}
						catch (Exception)
						{
							throw new TemplateException();
						}
					}
					else
					{
						if (!MatchCase)
							ret.options.Template = ret.options.Template.ToLower();
					}
				}
				return ret;
			}
		};

		public class BulkSearchState
		{
			internal IMatch searchMatch;
		};

		public struct MatchedTextRange
		{
			public int MatchBegin;
			public int MatchEnd;
			public bool WholeTextMatched;
		};

		public static MatchedTextRange? SearchInMessageText(MessageBase msg, PreprocessedOptions options, BulkSearchState bulkSearchState, int? startTextPosition = null)
		{
			Options opts = options.options;
			MessageBase.MessageFlag typeMask = options.typeMask;
			MessageBase.MessageFlag msgTypeMask = options.msgTypeMask;
			IRegex re = options.re;

			MessageBase.MessageFlag f = msg.Flags;
			if (opts.TypesToLookFor != MessageBase.MessageFlag.None) // None is treated as 'type selection isn't required'
			{
				if ((f & typeMask) == 0)
					return null;

				if (msgTypeMask != MessageBase.MessageFlag.None && (f & msgTypeMask) == 0)
					return null;
			}

			if (opts.SearchWithinThisThread != null)
				if (msg.Thread != opts.SearchWithinThisThread)
					return null;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of following after the last matched one
			bool wholeTextMatched = false;

			StringSlice text = msg.Text;

			if (!string.IsNullOrEmpty(opts.Template)) // empty/null template means that text matching isn't required
			{
				int textPos;

				if (startTextPosition.HasValue)
					textPos = startTextPosition.Value;
				else if (opts.ReverseSearch)
					textPos = text.Length;
				else
					textPos = 0;

				if (re != null)
				{
					if (!re.Match(text, textPos, ref bulkSearchState.searchMatch))
						return null;
					matchBegin = bulkSearchState.searchMatch.Index;
					matchEnd = matchBegin + bulkSearchState.searchMatch.Length;
				}
				else
				{
					StringComparison cmp = opts.MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
					int i;
					if (opts.ReverseSearch)
						i = text.LastIndexOf(opts.Template, textPos, cmp);
					else
						i = text.IndexOf(opts.Template, textPos, cmp);
					if (i < 0)
						return null;
					matchBegin = i;
					matchEnd = matchBegin + opts.Template.Length;
				}

				if (opts.WholeWord)
				{
					if (matchBegin > 0)
						if (StringUtils.IsLetterOrDigit(text[matchBegin - 1]))
							return null;
					if (matchEnd < text.Length - 1)
						if (StringUtils.IsLetterOrDigit(text[matchEnd]))
							return null;
				}
			}
			else
			{
				matchBegin = 0;
				matchEnd = text.Length;
				wholeTextMatched = true;
			}

			return new MatchedTextRange() { MatchBegin = matchBegin, MatchEnd = matchEnd, WholeTextMatched = wholeTextMatched };
		}
	};
}
