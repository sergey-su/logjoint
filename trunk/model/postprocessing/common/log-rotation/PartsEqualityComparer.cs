using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
    class PartsOfSameLogEqualityComparer : IEqualityComparer<ILogPartToken>
    {
        bool IEqualityComparer<ILogPartToken>.Equals(ILogPartToken? x, ILogPartToken? y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.CompareTo(y) != 0;
        }

        int IEqualityComparer<ILogPartToken>.GetHashCode(ILogPartToken obj)
        {
            return 0; // all tokens will have same hash code to force slow comparison via Equals(x, y)
        }
    };
}
