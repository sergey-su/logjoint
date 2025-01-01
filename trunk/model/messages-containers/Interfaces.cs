using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
    public interface IMessagesCollection
    {
        int Count { get; }
        IEnumerable<IndexedMessage> Forward(int begin, int end);
        IEnumerable<IndexedMessage> Reverse(int begin, int end);
    };

    public class TimeConstraintViolationException : InvalidOperationException
    {
        public IMessage ConflictingMessage1 { get; private set; }
        public IMessage ConflictingMessage2 { get; private set; }

        public TimeConstraintViolationException(IMessage m1 = null, IMessage m2 = null) :
            base("Time constraint violation.")
        {
            ConflictingMessage1 = m1;
            ConflictingMessage2 = m2;
        }
    };
}
