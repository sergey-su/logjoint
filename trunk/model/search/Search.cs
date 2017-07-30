using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using LogJoint.RegularExpressions;

namespace LogJoint
{
	public static class Search
	{
		public class SearchState
		{
			internal Options options;
			internal IRegex re;
			internal MessageFlag contentTypeMask;
			internal IMatch searchMatch;
		};

		public class TemplateException : Exception
		{
		};

		public struct Options
		{
			private MessageFlag contentTypes;
			private IFilterScope scope;

			public string Template;
			public bool WholeWord;
			public bool Regexp;
			public bool MatchCase;
			public bool ReverseSearch;
			public bool SearchInRawText;

			public static IEqualityComparer<Options> EqualityComparer = new EqualityComparerImp();

			public MessageFlag ContentTypes
			{
				get
				{
					if (contentTypes == MessageFlag.None) // zero (default-initialized) mask is treated as all-set mask
						return MessageFlag.ContentTypeMask;
					return contentTypes;
				}
				set
				{
					contentTypes = value & MessageFlag.ContentTypeMask;
				}
			}

			public static MessageFlag DefaultContentTypes => MessageFlag.ContentTypeMask;

			public IFilterScope Scope
			{
				get
				{
					return scope ?? FiltersFactory.DefaultScope;
				}
				set
				{
					scope = value;
				}
			}

			public void Save(XElement e)
			{
				e.Value = Template;
				if (Regexp)
					e.SetAttributeValue("regex", 1);
				if (WholeWord)
					e.SetAttributeValue("whole-word", 1);
				if (MatchCase)
					e.SetAttributeValue("match-case", 1);
				if (ContentTypes != DefaultContentTypes)
					e.SetAttributeValue("messages-types", (int)ContentTypes);
			}

			public Options Load(XElement e)
			{
				Template = e.Value;
				Regexp = e.AttributeValue("regex") == "1";
				WholeWord = e.AttributeValue("whole-word") == "1";
				MatchCase = e.AttributeValue("match-case") == "1";
				int typesAttrs;
				if (!int.TryParse(e.AttributeValue("messages-types"), out typesAttrs))
					typesAttrs = 0;
				contentTypes = ((MessageFlag)typesAttrs) & MessageFlag.ContentTypeMask;
				return this;
			}

			public Options SetSearchInRawText(bool value)
			{
				var tmp = this;
				tmp.SearchInRawText = value;
				return tmp;
			}

			/// <summary>
			/// Preprocesses the search options and returns an opaque object
			/// that holds the state needed to efficiently search many times using the search options.
			/// Different threads can not share the returned state object. Each thread has to call this method.
			/// </summary>
			public SearchState BeginSearch()
			{
				SearchState ret = new SearchState() { 
					options = this,
					contentTypeMask = MessageFlag.ContentTypeMask & ContentTypes,
				};
				if (!string.IsNullOrEmpty(Template))
				{
					if (Regexp)
					{
						ReOptions reOpts = ReOptions.AllowPatternWhitespaces;
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

			class EqualityComparerImp : IEqualityComparer<Options>
			{
				bool IEqualityComparer<Options>.Equals(Options x, Options y)
				{
					return
						x.MatchCase == y.MatchCase &&
						x.WholeWord == y.WholeWord &&
						x.Regexp == y.Regexp &&
						x.ContentTypes == y.ContentTypes &&
						x.ReverseSearch == y.ReverseSearch &&
						x.SearchInRawText == y.SearchInRawText &&
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
		};

		public struct MatchedTextRange
		{
			readonly public int MatchBegin;
			readonly public int MatchEnd;
			readonly public bool WholeTextMatched;
			readonly public StringSlice SourceText;

			public MatchedTextRange(StringSlice sourceText, int b, int e, bool wholeTextMatched)
			{
				this.SourceText = sourceText;
				this.MatchBegin = b;
				this.MatchEnd = e;
				this.WholeTextMatched = wholeTextMatched;
			}
		};

		public static MatchedTextRange? SearchInMessageText(
			IMessage msg, 
			SearchState state, 
			int? startTextPosition = null)
		{
			MessageFlag msgFlags = msg.Flags;
			var msgType = msgFlags & MessageFlag.TypeMask;
			if (msgType == MessageFlag.Content && (msgFlags & state.contentTypeMask) == 0)
			{
				return null;
			}

			if (!state.options.Scope.ContainsMessage(msg))
			{
				return null;
			}

			StringSlice sourceText;
			if (state.options.SearchInRawText)
			{
				sourceText = msg.RawText;
				if (!sourceText.IsInitialized)
					sourceText = msg.Text;
			}
			else
			{
				sourceText = msg.Text;
			}
			return SearchInText(sourceText, state, startTextPosition);
		}

		public static MatchedTextRange? SearchInText(StringSlice text, SearchState state, int? startTextPosition)
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
	};
}
