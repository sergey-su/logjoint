using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class SearchHistoryEntry : IEquatable<SearchHistoryEntry>
	{
		public readonly string Template;
		public readonly bool WholeWord;
		public readonly bool Regexp;
		public readonly bool MatchCase;
		public readonly MessageFlag TypesToLookFor;

		public bool IsValid { get { return !String.IsNullOrWhiteSpace(normalizedTemplate); } }

		public SearchHistoryEntry(Search.Options searchOptions)
		{
			Template = searchOptions.Template ?? "";
			WholeWord = searchOptions.WholeWord;
			Regexp = searchOptions.Regexp;
			MatchCase = searchOptions.MatchCase;
			TypesToLookFor = searchOptions.TypesToLookFor & (MessageFlag.TypeMask | MessageFlag.ContentTypeMask);
			InitNormalizedTemplate();
		}

		public SearchHistoryEntry(XElement e)
		{
			Template = e.Value;
			Regexp = e.AttributeValue("regex") == "1";
			WholeWord = e.AttributeValue("whole-word") == "1";
			MatchCase = e.AttributeValue("match-case") == "1";
			int typesAttrs;
			if (!int.TryParse(e.AttributeValue("messages-types"), out typesAttrs))
				typesAttrs = 0xffff;
			TypesToLookFor = ((MessageFlag)typesAttrs) & (MessageFlag.TypeMask | MessageFlag.ContentTypeMask);
			InitNormalizedTemplate();
		}

		public override int GetHashCode()
		{
			return normalizedTemplate.GetHashCode();
		}

		public bool Equals(SearchHistoryEntry other)
		{
			return normalizedTemplate == other.normalizedTemplate;
		}

		public override string ToString()
		{
			return Template;
		}

		public string Description
		{
			get
			{
				if (description == null)
				{
					var builder = new StringBuilder();
					builder.Append(Template);
					int flagIdx = 0;
					if (Regexp)
						AppendFlag(builder, "regexp", ref flagIdx);
					if (WholeWord)
						AppendFlag(builder, "whole word", ref flagIdx);
					if (MatchCase)
						AppendFlag(builder, "match case", ref flagIdx);
					if (flagIdx > 0)
						builder.Append(')');
					description = builder.ToString();
				}
				return description;
			}
		}

		public XElement Store()
		{
			var ret = new XElement("entry");
			ret.Value = Template;
			ret.SetAttributeValue("regex", Regexp ? 1 : 0);
			ret.SetAttributeValue("whole-word", WholeWord ? 1 : 0);
			ret.SetAttributeValue("match-case", MatchCase ? 1 : 0);
			ret.SetAttributeValue("messages-types", (int)TypesToLookFor);
			return ret;
		}

		static void AppendFlag(StringBuilder builder, string flag, ref int flagIdx)
		{
			if (flagIdx == 0)
				builder.Append(" (");
			else
				builder.Append(", ");
			builder.Append(flag);
			++flagIdx;
		}

		private void InitNormalizedTemplate()
		{
			normalizedTemplate = !MatchCase ? Template.ToLower() : Template;
		}

		private string description;
		private string normalizedTemplate;
	};

}
