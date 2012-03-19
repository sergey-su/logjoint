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
			public bool SearchInRawText;
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
			MessageBase.MessageFlag typeMask = options.typeMask;
			MessageBase.MessageFlag msgTypeMask = options.msgTypeMask;

			MessageBase.MessageFlag msgFlags = msg.Flags;
			if (options.options.TypesToLookFor != MessageBase.MessageFlag.None) // None means All
			{
				var msgType = msgFlags & typeMask;
				if (msgType == 0)
					return null;
				if (msgType == MessageBase.MessageFlag.Content && (msgFlags & msgTypeMask) == 0)
					return null;
			}

			if (options.options.SearchWithinThisThread != null)
				if (msg.Thread != options.options.SearchWithinThisThread)
					return null;

			return SearchInText(options.options.SearchInRawText ? msg.RawText : msg.Text, options, bulkSearchState, startTextPosition);
		}

		public static MatchedTextRange? SearchInText(StringSlice text, PreprocessedOptions options, BulkSearchState bulkSearchState, int? startTextPosition)
		{
			IRegex re = options.re;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of following after the last matched one
			bool wholeTextMatched = false;

			if (!string.IsNullOrEmpty(options.options.Template)) // empty/null template means that text matching isn't required
			{
				int textPos;

				if (startTextPosition.HasValue)
					textPos = startTextPosition.Value;
				else if (options.options.ReverseSearch)
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
					StringComparison cmp = options.options.MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
					int i;
					if (options.options.ReverseSearch)
						i = text.LastIndexOf(options.options.Template, textPos, cmp);
					else
						i = text.IndexOf(options.options.Template, textPos, cmp);
					if (i < 0)
						return null;
					matchBegin = i;
					matchEnd = matchBegin + options.options.Template.Length;
				}

				if (options.options.WholeWord)
				{
					if (matchBegin > 0)
						if (StringUtils.IsWordChar(text[matchBegin - 1]))
							return null;
					if (matchEnd < text.Length - 1)
						if (StringUtils.IsWordChar(text[matchEnd]))
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
