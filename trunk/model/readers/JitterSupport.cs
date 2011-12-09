using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace LogJoint
{
	public struct DejitteringParams
	{
		public int JitterBufferSize;

		public DejitteringParams(XElement configNode)
		{
			if (configNode == null)
				throw new ArgumentNullException("configNode");
			var a = configNode.Attribute("jitter-buffer-size");
			if (a == null || !int.TryParse(a.Value, out JitterBufferSize))
				JitterBufferSize = 16;
		}
		static public DejitteringParams? FromConfigNode(XElement configNode)
		{
			if (configNode == null)
				return null;
			return new DejitteringParams(configNode);
		}
	};

	/// <summary>
	/// Implementation of IPositionedMessagesParser that mitigates 'partially-sorted-log' problem.
	/// 'partially-sorted-log' problem has to do with logs that are mostly sorded by time but
	/// there might be little defects where several messages have incorrect order. Bad order 
	/// may be a result of bad logic in multithreded log writer. Well known example of a log
	/// having 'partially-sorted-log' problem is Windows Event Log.
	/// DejitteringMessagesParser is a transparent wrapper for underlying IPositionedMessagesParser.
	/// Logically DejitteringMessagesParser implements the following idea: when client reads Nth message 
	/// a range of messages is actually read (N - jitterBufferSize/2, N + jitterBufferSize/2). 
	/// This range is sorded by time and the message in the middle of the range is 
	/// returned as Nth message. DejitteringMessagesParser is optimized for sequential reading.
	/// </summary>
	public class DejitteringMessagesParser : IPositionedMessagesParser
	{
		public DejitteringMessagesParser(Func<CreateParserParams, IPositionedMessagesParser> underlyingParserFactory,
			CreateParserParams originalParams, DejitteringParams config): 
			this(underlyingParserFactory, originalParams, config.JitterBufferSize)
		{
		}

		public DejitteringMessagesParser(Func<CreateParserParams, IPositionedMessagesParser> underlyingParserFactory, 
			CreateParserParams originalParams, int jitterBufferSize)
		{
			if (jitterBufferSize < 1)
				throw new ArgumentException("jitterBufferSize must be equal to or geater than 1");
			if (underlyingParserFactory == null)
				throw new ArgumentNullException("underlyingParserFactory");
			if (originalParams.Range == null)
				throw new ArgumentNullException("DejitteringMessagesParser does not support unspecified positions range", "originalParams.Range");

			this.originalParams = originalParams;
			this.originalParams.EnsureStartPositionIsInRange();

			this.jitterBufferSize = jitterBufferSize;
			this.jitterBuffer = new VCSKicksCollection.PriorityQueue<PostprocessedMessage>(new Comparer(originalParams.Direction));
			this.positionsBuffer = new Generic.CircularBuffer<long>(jitterBufferSize + 1);
			CreateUnderlyingParserAndInitJitterBuffer(underlyingParserFactory);
		}

		#region IPositionedMessagesParser Members

		public MessageBase ReadNext()
		{
			return ReadNextAndPostprocess().Message;
		}

		public PostprocessedMessage ReadNextAndPostprocess()
		{
			CheckDisposed();
			var ret = jitterBuffer.Dequeue();
			if (ret.Message != null)
			{
				ret.Message.SetPosition(positionsBuffer.Pop());
				if (!originalParams.Range.Value.IsInRange(ret.Message.Position))
				{
					return new PostprocessedMessage();
				}
			}
			LoadNextMessage();
			return ret;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			enumerator.Dispose();
		}

		#endregion

		class Comparer : IComparer<PostprocessedMessage>
		{
			int inversionFlag;

			public Comparer(MessagesParserDirection direction)
			{
				inversionFlag = direction == MessagesParserDirection.Forward ? 1 : -1;
			}

			#region IComparer<MessageBase> Members

			public int Compare(PostprocessedMessage x, PostprocessedMessage y)
			{
				int cmpResult;

				cmpResult = inversionFlag * Math.Sign((x.Message.Time - y.Message.Time).Ticks);
				if (cmpResult != 0)
					return cmpResult;

				cmpResult = inversionFlag * Math.Sign(x.Message.Position - y.Message.Position);
				return cmpResult;
			}

			#endregion
		};

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		static MessagesParserDirection GetOppositeDirection(MessagesParserDirection direction)
		{
			return direction == MessagesParserDirection.Backward ? MessagesParserDirection.Forward : MessagesParserDirection.Backward;
		}

		void CreateUnderlyingParserAndInitJitterBuffer(Func<CreateParserParams, IPositionedMessagesParser> underlyingParserFactory)
		{
			CreateParserParams reversedParserParams = originalParams;
			reversedParserParams.Range = null;
			reversedParserParams.Direction = GetOppositeDirection(originalParams.Direction);
			reversedParserParams.Flags |= MessagesParserFlag.DisableMultithreading;

			int reversedMessagesQueued = 0;

			using (IPositionedMessagesParser reversedParser = underlyingParserFactory(reversedParserParams))
			{
				var tmp = new List<PostprocessedMessage>();
				for (int i = 0; i < jitterBufferSize; ++i)
				{
					var tmpMsg = reversedParser.ReadNextAndPostprocess();
					if (tmpMsg.Message == null)
						break;
					tmp.Add(tmpMsg);
				}
				tmp.Reverse();
				foreach (var tmpMsg in tmp)
				{
					jitterBuffer.Enqueue(tmpMsg);
					positionsBuffer.Push(tmpMsg.Message.Position);
					++reversedMessagesQueued;
				}
			}

			enumerator = ReadAddMessagesFromRangeCompleteJitterBuffer(underlyingParserFactory).GetEnumerator();
			for (int i = 0; i < jitterBufferSize; ++i)
			{
				var tmp = LoadNextMessage();
				reversedMessagesQueued -= tmp.DequeuedMessages;
				if (tmp.LoadedMessage == null)
					break;
			}
			for (int i = 0; i < reversedMessagesQueued && jitterBuffer.Count > 0; ++i)
			{
				jitterBuffer.Dequeue();
				positionsBuffer.Pop();
			}
		}

		IEnumerable<PostprocessedMessage> ReadAddMessagesFromRangeCompleteJitterBuffer(Func<CreateParserParams, IPositionedMessagesParser> underlyingParserFactory)
		{
			CreateParserParams mainParserParams = originalParams;
			//mainParserParams.Range = null;
			using (var mainParser = underlyingParserFactory(mainParserParams))
			{
				for (; ; )
				{
					var msg = mainParser.ReadNextAndPostprocess();
					if (msg.Message == null)
						break;
					yield return msg;
				}
			}

			CreateParserParams jitterBufferCompletionParams = originalParams;
			jitterBufferCompletionParams.Flags |= MessagesParserFlag.DisableMultithreading;
			jitterBufferCompletionParams.Range = null;
			jitterBufferCompletionParams.StartPosition = originalParams.Direction == MessagesParserDirection.Forward ? originalParams.Range.Value.End : originalParams.Range.Value.Begin;
			using (var completionParser = underlyingParserFactory(jitterBufferCompletionParams))
			{
				for (int i = 0; i < jitterBufferSize; ++i)
				{
					var msg = completionParser.ReadNextAndPostprocess();
					if (msg.Message == null)
						break;
					yield return msg;
				}
			}
		}

		struct LoadNextMessageResult
		{
			public MessageBase LoadedMessage;
			public int DequeuedMessages;
		};

		LoadNextMessageResult LoadNextMessage()
		{
			LoadNextMessageResult ret = new LoadNextMessageResult();
			if (eofReached)
				return ret;
			if (!enumerator.MoveNext())
			{
				eofReached = true;
			}
			else
			{
				var tmp = enumerator.Current;
				ret.LoadedMessage = tmp.Message;
				jitterBuffer.Enqueue(tmp);
				positionsBuffer.Push(tmp.Message.Position);
				if (jitterBuffer.Count > jitterBufferSize)
				{
					jitterBuffer.Dequeue();
					positionsBuffer.Pop();
					ret.DequeuedMessages = 1;
				}
			}
			return ret;
		}

		readonly CreateParserParams originalParams;
		readonly VCSKicksCollection.PriorityQueue<PostprocessedMessage> jitterBuffer;
		readonly Generic.CircularBuffer<long> positionsBuffer;
		readonly int jitterBufferSize;
		IEnumerator<PostprocessedMessage> enumerator;
		bool eofReached;
		bool disposed;
	}
}
