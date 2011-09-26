using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	public static class MessagesParserToEnumerator
	{
		public static IEnumerable<MessageBase> ParserAsEnumerator(IPositionedMessagesParser parser)
		{
			for (; ; )
			{
				var msg = parser.ReadNext();
				if (msg == null)
					break;
				yield return msg;
			}
		}

		public static IPositionedMessagesParser EnumeratorAsParser(IEnumerable<MessageBase> enumerable)
		{
			return new EnumeratorAsParserImpl(enumerable);
		}

		class EnumeratorAsParserImpl : IPositionedMessagesParser
		{
			public EnumeratorAsParserImpl(IEnumerable<MessageBase> enumerable)
			{
				if (enumerable == null)
					throw new ArgumentNullException("enumerable");
				this.enumerable = enumerable;
			}

			public MessageBase ReadNext()
			{
				if (disposed)
					throw new ObjectDisposedException("EnumeratorAsParser");
				if (enumerator == null)
					enumerator = enumerable.GetEnumerator();
				if (!enumerator.MoveNext())
					return null;
				return enumerator.Current;
			}

			public void Dispose()
			{
				if (disposed)
					return;
				disposed = true;
				if (enumerator != null)
					enumerator.Dispose();
			}

			readonly IEnumerable<MessageBase> enumerable;
			IEnumerator<MessageBase> enumerator;
			bool disposed;
		};
	}
}
