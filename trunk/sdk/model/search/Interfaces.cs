using System;

namespace LogJoint.Search
{
	public struct Options
	{
		private MessageFlag contentTypes;
		private IFilterScope scope;
		public MessageTextGetter messageTextGetter;

		public string Template;
		public bool WholeWord;
		public bool Regexp;
		public bool MatchCase;
		public bool ReverseSearch;
		public MessageTextGetter MessageTextGetter
		{
			get => messageTextGetter ?? MessageTextGetters.SummaryTextGetter;
			set => messageTextGetter = value ?? throw new ArgumentNullException();
		}

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
				return scope ?? FilterScope.DefaultScope;
			}
			set
			{
				scope = value;
			}
		}
	};

	public class TemplateException : Exception
	{
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

}
