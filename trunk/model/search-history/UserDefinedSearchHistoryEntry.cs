using System;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
    public class UserDefinedSearchHistoryEntry : IUserDefinedSearchHistoryEntry
    {
        readonly IUserDefinedSearch uds;

        public UserDefinedSearchHistoryEntry(IUserDefinedSearch uds)
        {
            this.uds = uds;
        }

        public static UserDefinedSearchHistoryEntry TryLoad(XElement e, IUserDefinedSearches udss)
        {
            if (e.AttributeValue("type") != "uds")
                return null;
            var name = e.AttributeValue("name");
            var uds = udss.Items.FirstOrDefault(i => i.Name == name);
            return new UserDefinedSearchHistoryEntry(uds);
        }

        bool ISearchHistoryEntry.IsValid { get { return uds != null; } }

        void ISearchHistoryEntry.Save(XElement e)
        {
            e.SetAttributeValue("type", "uds");
            e.SetAttributeValue("name", uds.Name);
        }

        IUserDefinedSearch IUserDefinedSearchHistoryEntry.UDS => uds;

        bool IEquatable<ISearchHistoryEntry>.Equals(ISearchHistoryEntry other)
        {
            var e = other as UserDefinedSearchHistoryEntry;
            if (e == null)
                return false;
            return ReferenceEquals(e.uds, uds);
        }

        public override string ToString()
        {
            return uds?.Name;
        }
    };
}
