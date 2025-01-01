using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
    public interface IPrefixMatcher
    {
        int RegisterPrefix(string prefix);
        void Freeze();
        IMatchedPrefixesCollection Match(string str);
    };


    public interface IMatchedPrefixesCollection : IEnumerable<int>
    {
        bool Contains(int prefixId);
    };

    public struct MessagePrefixesPair<M>
    {
        public readonly M Message;
        public readonly IMatchedPrefixesCollection Prefixes;

        public MessagePrefixesPair(M m, IMatchedPrefixesCollection prefixes)
        {
            Message = m;
            Prefixes = prefixes;
        }
    };
}
