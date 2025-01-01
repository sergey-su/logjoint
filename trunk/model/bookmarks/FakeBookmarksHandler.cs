using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace LogJoint
{
    public class DummyBookmarksHandler : IBookmarksHandler
    {
        bool IBookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(IMessage l, int lineIndex)
        {
            return false;
        }

        void IDisposable.Dispose()
        {
        }
    }
}
