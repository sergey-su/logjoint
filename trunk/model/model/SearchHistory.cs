using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
	public class SearchHistory
	{
		public SearchHistory(Persistence.IStorageEntry globalSettings)
		{
			this.globalSettings = globalSettings;

			LoadSearchHistory();
		}

		public static readonly string SettingsKey = "search-history";

		public class SearchHistoryEntry: IEquatable<SearchHistoryEntry>
		{
			public readonly string Template;
			public readonly bool WholeWord;
			public readonly bool Regexp;
			public readonly bool MatchCase;
			public readonly MessageBase.MessageFlag TypesToLookFor;

			public bool IsValid { get { return !String.IsNullOrWhiteSpace(normalizedTemplate); } }

			public SearchHistoryEntry(Search.Options searchOptions)
			{
				Template = searchOptions.Template ?? "";
				WholeWord = searchOptions.WholeWord;
				Regexp = searchOptions.Regexp;
				MatchCase = searchOptions.MatchCase;
				TypesToLookFor = searchOptions.TypesToLookFor & (MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask);
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
				TypesToLookFor = ((MessageBase.MessageFlag)typesAttrs) & (MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask);
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

		public event EventHandler OnChanged;

		static public int MaxItemsCount
		{
			get { return 15; }
		}
		public void Add(SearchHistoryEntry entry)
		{
			if (entry.Template.Length == 0)
				return;
			items.RemoveAll(i => i.Equals(entry));
			if (items.Count >= MaxItemsCount)
				items.RemoveAt(0);
			items.Add(entry);
			FireOnChange();
			SaveSearchHistory();
		}
		public IEnumerable<SearchHistoryEntry> Items
		{
			get
			{
				for (int i = items.Count - 1; i >= 0; --i)
					yield return items[i]; 
			}
		}

		void FireOnChange()
		{
			if (OnChanged != null)
				OnChanged(this, EventArgs.Empty);
		}

		private void LoadSearchHistory()
		{
			using (var section = globalSettings.OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				items.AddRange(
					from entryNode in section.Data.Elements(rootNodeName).Elements(entryNodeName)
					let entry = new SearchHistoryEntry(entryNode)
					where entry.IsValid
					select entry
				);
			}
		}

		private void SaveSearchHistory()
		{
			using (var section = globalSettings.OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadWrite))
			{
				var newContent = section.Data.Elements(rootNodeName).Elements(entryNodeName).Select(n => new SearchHistoryEntry(n)).Union(items).Distinct().Select(e => e.Store()).ToArray();
				section.Data.RemoveNodes();
				section.Data.Add(new XElement(rootNodeName, newContent));
			}
		}

		private readonly Persistence.IStorageEntry globalSettings;
		private readonly static string rootNodeName = "search-history";
		private readonly static string entryNodeName = "entry";
		private readonly List<SearchHistoryEntry> items = new List<SearchHistoryEntry>(MaxItemsCount);
	}
}
