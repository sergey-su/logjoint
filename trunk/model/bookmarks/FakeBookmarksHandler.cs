using System; 
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace LogJoint
{
	public class DummyBookmarksHandler: IBookmarksHandler
	{
		bool IBookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(IMessage l)
		{
			return false;
		}

		void IDisposable.Dispose()
		{
		}
	}
}
