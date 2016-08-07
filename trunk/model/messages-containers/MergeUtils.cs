using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint.MessagesContainers
{
	static class MergeUtils
	{
		public static IEnumerable<PostprocessedMessage> MergePostprocessedMessage(IEnumerable<PostprocessedMessage>[] enums)
		{
			var comparer = new EnumeratorsComparer();
			var iters = new VCSKicksCollection.PriorityQueue<IEnumerator<PostprocessedMessage>>(comparer);
			try
			{
				foreach (var e in enums)
				{
					var i = e.GetEnumerator();
					if (i.MoveNext())
						iters.Enqueue(i);
				}
				for (; iters.Count > 0; )
				{
					var i = iters.Dequeue();
					try
					{
						yield return i.Current;
						if (i.MoveNext())
						{
							iters.Enqueue(i);
							i = null;
						}
					}
					finally
					{
						if (i != null)
							i.Dispose();
					}
				}
			}
			finally
			{
				while (iters.Count != 0)
					iters.Dequeue().Dispose();
			}
		}

		class EnumeratorsComparer : IComparer<IEnumerator<PostprocessedMessage>>
		{
			public int Compare(IEnumerator<PostprocessedMessage> x, IEnumerator<PostprocessedMessage> y)
			{
				return MessagesComparer.Compare(x.Current.Message, y.Current.Message);
			}
		};
	};

}
