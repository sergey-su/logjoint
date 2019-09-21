using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace LogJoint
{
	public static class LogSourcesManagerExtensions
	{
		class EnumMessagesHelper : IDisposable
		{
			readonly ILogSource ls;
			const int maxBufferSize = 128 * 1024;
			readonly Queue<IMessage> buffer = new Queue<IMessage>();
			readonly CancellationToken cancellation;
			readonly Progress.IProgressEventsSink progress;
			readonly FileRange.Range lsRange;
			IMessage peek;
			long lastReadPositon;
			bool eof;
			bool disposed;

			public EnumMessagesHelper(
				ILogSource ls,
				CancellationToken cancellation,
				Progress.IProgressEventsSink progress
			)
			{
				this.ls = ls;
				this.cancellation = cancellation;
				this.progress = progress;
				this.lsRange = ls.Provider.Stats.PositionsRange;
				this.lastReadPositon = lsRange.Begin;
			}

			public void Dispose()
			{
				if (!disposed)
				{
					progress.Dispose();
					disposed = true;
				}
			}

			public IMessage Peek()
			{
				return peek;
			}

			public void Dequeue()
			{
				buffer.Dequeue();
				UpdatePeek();
			}

			public async Task FillBuffer()
			{
				if (eof)
					return;
				for (; ; )
				{
					var tmp = new List<IMessage>();
					var tmpLastPosition = lastReadPositon;
					try
					{
						await ls.Provider.EnumMessages(
							lastReadPositon,
							m =>
							{
								if (tmp.Count >= maxBufferSize)
									return false;
								tmp.Add(m);
								tmpLastPosition = m.EndPosition;
								return true;
							},
							EnumMessagesFlag.Forward | EnumMessagesFlag.IsSequentialScanningHint,
							LogProviderCommandPriority.BackgroundActivity,
							cancellation
						);
					}
					catch (OperationCanceledException)
					{
						if (cancellation.IsCancellationRequested)
							throw;
						await Task.Delay(100);
						continue;
					}
					eof = tmp.Count < maxBufferSize;
					foreach (var x in tmp)
						buffer.Enqueue(x);
					lastReadPositon = tmpLastPosition;
					UpdatePeek();
					if (lsRange.Length > 0)
					{
						progress.SetValue((double)this.lastReadPositon / (double)lsRange.Length);
					}
					break;
				}
			}

			void UpdatePeek()
			{
				peek = buffer.Count > 0 ? buffer.Peek() : null;
			}

			public class Comparer : IComparer<EnumMessagesHelper>
			{
				int IComparer<EnumMessagesHelper>.Compare(EnumMessagesHelper x, EnumMessagesHelper y)
				{
					return MessagesComparer.Compare(x.Peek(), y.Peek());
				}
			}
		};

		static async Task EnumMessagesAndMerge(
			ILogSource[] sources,
			Action<IMessage> callback,
			Progress.IProgressAggregator progress,
			CancellationToken cancellation
		)
		{
			var queue = new VCSKicksCollection.PriorityQueue<EnumMessagesHelper>(
				new EnumMessagesHelper.Comparer());
			var helpers = sources.Select(s => new EnumMessagesHelper(
				s, cancellation, progress.CreateProgressSink())).ToList();
			try
			{
				await Task.WhenAll(helpers.Select(h => h.FillBuffer()));
				void enqueueOrKill(EnumMessagesHelper h)
				{
					if (h.Peek() != null)
						queue.Enqueue(h);
					else
						h.Dispose();
				}
				helpers.ForEach(enqueueOrKill);
				while (queue.Count > 0)
				{
					var h = queue.Dequeue();
					callback(h.Peek());
					h.Dequeue();
					if (h.Peek() == null)
						await h.FillBuffer();
					enqueueOrKill(h);
				}
			}
			finally
			{
				helpers.ForEach(h => h.Dispose());
			}
		}

		public static async Task SaveJoinedLog(
			ILogSourcesManager sources,
			CancellationToken cancel,
			Progress.IProgressAggregator progress,
			string fileName
		)
		{
			var visibleSources = sources.Items.Where(s => s.Visible).ToArray();
			using (var fs = new StreamWriter(fileName, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
			{
				await EnumMessagesAndMerge(
					visibleSources,
					m =>
					{
						var txt = m.RawText.IsInitialized ? m.RawText : m.Text;
						fs.Write(txt.ToString());
						fs.Write("\n");
					},
					progress,
					cancel
				);
			}
		}
	};
}
