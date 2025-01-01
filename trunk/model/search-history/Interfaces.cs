using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint
{
    public interface ISearchHistory
    {
        event EventHandler OnChanged;
        void Add(ISearchHistoryEntry entry);
        IEnumerable<ISearchHistoryEntry> Items { get; }
        int Count { get; }
        int MaxCount { get; set; }
        void Clear();
    };

    public interface ISearchHistoryEntry : IEquatable<ISearchHistoryEntry>
    {
        bool IsValid { get; }
        void Save(XElement e);
    };

    public interface ISimpleSearchHistoryEntry : ISearchHistoryEntry
    {
        Search.Options Options { get; }
    };

    public interface IUserDefinedSearchHistoryEntry : ISearchHistoryEntry
    {
        IUserDefinedSearch UDS { get; }
    };
}
