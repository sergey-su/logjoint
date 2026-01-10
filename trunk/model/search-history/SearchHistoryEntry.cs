using System;
using System.Text;
using System.Xml.Linq;
using LogJoint.Search;

namespace LogJoint
{
    public class SearchHistoryEntry : ISimpleSearchHistoryEntry
    {
        readonly string Template;
        readonly bool WholeWord;
        readonly bool Regexp;
        readonly bool MatchCase;
        readonly MessageFlag TypesToLookFor;
        readonly string normalizedTemplate;

        public SearchHistoryEntry(Search.Options searchOptions)
        {
            Template = searchOptions.Template ?? "";
            WholeWord = searchOptions.WholeWord;
            Regexp = searchOptions.Regexp;
            MatchCase = searchOptions.MatchCase;
            TypesToLookFor = searchOptions.ContentTypes;

            this.normalizedTemplate = !MatchCase ? Template.ToLower() : Template;
        }

        public SearchHistoryEntry(XElement e) : this(new Search.Options().Load(e))
        {
        }

        bool ISearchHistoryEntry.IsValid { get { return !String.IsNullOrWhiteSpace(normalizedTemplate); } }

        void ISearchHistoryEntry.Save(XElement e)
        {
            ToSearchOptions().Save(e);
        }

        Search.Options ISimpleSearchHistoryEntry.Options => ToSearchOptions();

        bool IEquatable<ISearchHistoryEntry>.Equals(ISearchHistoryEntry? other)
        {
            var e = other as SearchHistoryEntry;
            if (e == null)
                return false;
            return normalizedTemplate == e.normalizedTemplate;
        }

        public override string ToString()
        {
            return Template;
        }

        Search.Options ToSearchOptions()
        {
            return new Search.Options()
            {
                Template = Template,
                WholeWord = WholeWord,
                Regexp = Regexp,
                MatchCase = MatchCase,
                ContentTypes = TypesToLookFor
            };
        }
    };
}
