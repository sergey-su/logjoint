using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace LogJoint
{
	public static class LogSourceExtensions
	{
		public static string GetShortDisplayNameWithAnnotation(this ILogSource ls)
		{
			if (ls.IsDisposed)
				return "";
			var logSourceName = ls.DisplayName;
			try
			{
				logSourceName = Path.GetFileName(logSourceName); // try to shorten long path
			}
			catch (ArgumentException)
			{
			}
			if (!string.IsNullOrEmpty(ls.Annotation))
				return ls.Annotation + "  " + logSourceName;
			return logSourceName;
		}

		public static string GetSafeConnectionId(this ILogSource ls)
		{
			if (ls.IsDisposed)
				return "";
			return ls.Provider.ConnectionId;
		}

		public static ILogSource FindLiveLogSourceOrCreateNew(
			this ILogSourcesManager logSources,
			ILogProviderFactory factory, 
			IConnectionParams cp)
		{
			ILogSource src = logSources.Find(cp);
			if (src != null && src.Provider.Stats.State == LogProviderState.LoadError)
			{
				src.Dispose();
				src = null;
			}
			if (src == null)
			{
				src = logSources.Create(factory, cp);
			}
			return src;
		}

		public static async Task<IBookmark> CreateTogglableBookmark(
			this ILogSource ls, 
			IBookmarksFactory factory,
			IBookmark sourceBookmark,
			CancellationToken cancallation
		)
		{
			if (sourceBookmark.LogSourceConnectionId != ls.Provider.ConnectionId)
				throw new ArgumentException("log source and bookmark have inconsistent connection ids");
			IMessage messageAtPosition = null;
			await ls.Provider.EnumMessages(
				sourceBookmark.Position,
				msg => 
				{
					if (msg.Position == sourceBookmark.Position)
						messageAtPosition = msg;
					return false;
				},
				EnumMessagesFlag.Forward,
				LogProviderCommandPriority.RealtimeUserAction,
				cancallation
			);
			if (messageAtPosition == null)
				return null;
			return factory.CreateBookmark(messageAtPosition, sourceBookmark.LineIndex, true);
		}

		public static async Task DeleteLogs(this ILogSourcesManager lsm, ILogSource[] sources)
		{
			var tasks = sources.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteAllLogs(this ILogSourcesManager lsm)
		{
			await DeleteLogs(lsm, lsm.Items.ToArray());
		}

		public static bool LogSourceStateIsOkToChangePersistentState(this ILogSource s)
		{
			if (s == null || s.IsDisposed)
				return false;
			if (s.Provider == null || s.Provider.IsDisposed)
				return false;
			var state = s.Provider.Stats.State;
			if (state == LogProviderState.LoadError || state == LogProviderState.NoFile)
				return false;
			return true;
		}

		class EnumMessagesHelper: IDisposable
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
				for (;;)
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
				Action<EnumMessagesHelper> enqueueOfKill = h =>
				{
					if (h.Peek() != null)
						queue.Enqueue(h);
					else
						h.Dispose();
				};
				helpers.ForEach(enqueueOfKill);
				while (queue.Count > 0)
				{
					var h = queue.Dequeue();
					callback(h.Peek());
					h.Dequeue();
					if (h.Peek() == null)
						await h.FillBuffer();
					enqueueOfKill(h);
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
			var visibleSources = sources.Items.Where(s => !s.IsDisposed && s.Visible).ToArray();
			using (var fs = new StreamWriter(fileName, false, Encoding.UTF8))
			{
				await EnumMessagesAndMerge(
					visibleSources,
					m => 
					{
						var txt = m.RawText.IsInitialized ? m.RawText : m.Text;
						fs.WriteLine(txt.ToString());
					},
					progress,
					cancel
				);
			}
		}

	}
}
