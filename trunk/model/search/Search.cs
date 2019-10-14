using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using LogJoint.RegularExpressions;

namespace LogJoint.Search
{
	public class SearchState
	{
		internal Options options;
		internal IRegex re;
		internal MessageFlag contentTypeMask;
		internal IMatch searchMatch;
	};

	public static class Extensions
	{
		private static readonly bool useRegexsForSimpleTemplates =
			RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? true : // on mac compiled regex seems to work faster than IndexOf
			false;

		public static MatchedTextRange? SearchInMessageText(
			this SearchState state,
			IMessage msg, 
			int? startTextPosition = null)
		{
			MessageFlag msgFlags = msg.Flags;
			if ((msgFlags & state.contentTypeMask) == 0)
			{
				return null;
			}

			if (!state.options.Scope.ContainsMessage(msg))
			{
				return null;
			}

			StringSlice sourceText = state.options.MessageTextGetter(msg).Text;
			return SearchInText(state, sourceText, startTextPosition);
		}

		public static MatchedTextRange? SearchInText(
			this SearchState state,
			StringSlice text,
			int? startTextPosition)
		{
			IRegex re = state.re;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of following after the last matched one
			bool wholeTextMatched = false;

			if (!string.IsNullOrEmpty(state.options.Template)) // empty/null template means that text matching isn't required, i.e. match any input
			{
				int textPos;

				if (startTextPosition.HasValue)
					textPos = startTextPosition.Value;
				else if (state.options.ReverseSearch)
					textPos = text.Length;
				else
					textPos = 0;

				for (; ; )
				{
					if (re != null)
					{
						if (!re.Match(text, textPos, ref state.searchMatch))
							return null;
						matchBegin = state.searchMatch.Index;
						matchEnd = matchBegin + state.searchMatch.Length;
					}
					else
					{
						StringComparison cmp = state.options.MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
						int i;
						// todo: use running hash
						if (state.options.ReverseSearch)
							i = text.LastIndexOf(state.options.Template, textPos, cmp);
						else
							i = text.IndexOf(state.options.Template, textPos, cmp);
						if (i < 0)
							return null;
						matchBegin = i;
						matchEnd = matchBegin + state.options.Template.Length;
					}

					if (state.options.WholeWord && !IsWordBoundary(text, matchBegin, matchEnd))
					{
						textPos = state.options.ReverseSearch ? matchBegin : matchEnd;
						continue;
					}

					break;
				}
			}
			else
			{
				matchBegin = 0;
				matchEnd = text.Length;
				wholeTextMatched = true;
			}

			return new MatchedTextRange(text, matchBegin, matchEnd, wholeTextMatched);
		}

		public static bool IsWordBoundary(this StringSlice stringSlice, int substringBegin, int substringEnd)
		{
			if (substringBegin > 0)
				if (StringUtils.IsWordChar(stringSlice[substringBegin - 1]))
					return false;
			if (substringEnd <= stringSlice.Length - 1)
				if (StringUtils.IsWordChar(stringSlice[substringEnd]))
					return false;
			return true;
		}

		public static void Save(this Options options, XElement e)
		{
			e.Value = options.Template;
			if (options.Regexp)
				e.SetAttributeValue("regex", 1);
			if (options.WholeWord)
				e.SetAttributeValue("whole-word", 1);
			if (options.MatchCase)
				e.SetAttributeValue("match-case", 1);
			if (options.ContentTypes != Options.DefaultContentTypes)
				e.SetAttributeValue("messages-types", (int)options.ContentTypes);
		}

		public static Options Load(this Options options, XElement e)
		{
			options.Template = e.Value;
			options.Regexp = e.AttributeValue("regex") == "1";
			options.WholeWord = e.AttributeValue("whole-word") == "1";
			options.MatchCase = e.AttributeValue("match-case") == "1";
			int typesAttrs;
			if (!int.TryParse(e.AttributeValue("messages-types"), out typesAttrs))
				typesAttrs = 0;
			options.ContentTypes = ((MessageFlag)typesAttrs) & MessageFlag.ContentTypeMask;
			return options;
		}

		/// <summary>
		/// Preprocesses the search options and returns an opaque object
		/// that holds the state needed to efficiently search many times using the search options.
		/// Different threads can not share the returned state object. Each thread has to call this method.
		/// </summary>
		public static SearchState BeginSearch(this Options options, IRegexFactory regexFactory, bool timeboxedMatching = false)
		{
			SearchState ret = new SearchState()
			{
				options = options,
				contentTypeMask = MessageFlag.ContentTypeMask & options.ContentTypes,
			};
			if (!string.IsNullOrEmpty(options.Template))
			{
				if (options.Regexp || useRegexsForSimpleTemplates)
				{
					ReOptions reOpts = ReOptions.AllowPatternWhitespaces;
					if (!options.MatchCase)
						reOpts |= ReOptions.IgnoreCase;
					if (options.ReverseSearch)
						reOpts |= ReOptions.RightToLeft;
					if (timeboxedMatching)
						reOpts |= ReOptions.Timeboxed;
					try
					{
						ret.re = regexFactory.Create(
							options.Regexp ? options.Template : System.Text.RegularExpressions.Regex.Escape(options.Template), reOpts);
					}
					catch (Exception)
					{
						throw new TemplateException();
					}
				}
				else
				{
					if (!options.MatchCase)
						ret.options.Template = ret.options.Template.ToLower();
				}
			}
			return ret;
		}
	};

	internal class EqualityComparer : IEqualityComparer<Options>
	{
		public static IEqualityComparer<Options> Instance = new EqualityComparer();

		bool IEqualityComparer<Options>.Equals(Options x, Options y)
		{
			return
				x.MatchCase == y.MatchCase &&
				x.WholeWord == y.WholeWord &&
				x.Regexp == y.Regexp &&
				x.ContentTypes == y.ContentTypes &&
				x.ReverseSearch == y.ReverseSearch &&
				x.MessageTextGetter == y.MessageTextGetter &&
				GetTemplateComparer(x.MatchCase).Equals(x.Template, y.Template) &&
				x.Scope.Equals(y.Scope);
		}

		int IEqualityComparer<Options>.GetHashCode(Options obj)
		{
			return
				Hashing.GetHashCode(GetTemplateComparer(obj.MatchCase).GetHashCode(obj.Template),
				Hashing.GetHashCode(obj.WholeWord.GetHashCode(),
				Hashing.GetHashCode(obj.MatchCase.GetHashCode(),
				Hashing.GetHashCode(obj.ContentTypes.GetHashCode(),
				Hashing.GetHashCode(obj.Scope.GetHashCode()
			)))));
		}

		static StringComparer GetTemplateComparer(bool matchCase)
		{
			return matchCase ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
		}
	}
}
