using System;
using System.Threading.Tasks;

namespace LogJoint
{
	internal class EnumMessagesCommand : IAsyncLogProviderCommandHandler
	{
		public EnumMessagesCommand(long startFrom, EnumMessagesFlag flags, Func<IMessage, bool> callback)
		{
			this.flags = flags;
			this.startFrom = startFrom;
			this.positionToContinueAsync = startFrom;
			this.callback = callback;
			this.direction = (flags & EnumMessagesFlag.Backward) != 0 ?
				MessagesParserDirection.Backward : MessagesParserDirection.Forward;
		}

		public Task Task { get { return task.Task; } }

		public override string ToString()
		{
			return string.Format("{0} {1}", startFrom, flags);
		}

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			if (ctx.Cache == null)
				return false;

			var cache = ctx.Cache;
			if (direction == MessagesParserDirection.Forward && startFrom >= ctx.Stats.PositionsRange.End)
				return true;
			if (direction == MessagesParserDirection.Backward && startFrom <= ctx.Stats.PositionsRange.Begin)
				return true;

			bool finishedSynchroniously = false;
			var testRange = direction == MessagesParserDirection.Forward ?
				cache.MessagesRange : cache.MessagesRange.ChangeDirection();
			if (testRange.IsInRange(startFrom))
			{
				foreach (var i in (direction == MessagesParserDirection.Forward ? ctx.Cache.Messages.Forward(startFrom) : ctx.Cache.Messages.Reverse(startFrom)))
				{
					finishedSynchroniously = !callback(i.Message);
					if (finishedSynchroniously)
						break;
					ctx.Cancellation.ThrowIfCancellationRequested();
					positionToContinueAsync = direction == MessagesParserDirection.Forward ?
						i.Message.EndPosition : i.Message.Position - 1;
				}
				if (!finishedSynchroniously)
				{
					if (direction == MessagesParserDirection.Backward)
						// example: reading from position AvailableRange.Begin+1
						finishedSynchroniously = ctx.Cache.MessagesRange.Begin == ctx.Stats.PositionsRange.Begin;
					else if (direction == MessagesParserDirection.Forward)
						// example: reading from position AvailableRange.End-1
						finishedSynchroniously = ctx.Cache.MessagesRange.End == ctx.Stats.PositionsRange.End;
				}
			}
			return finishedSynchroniously;
		}

		async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			var parserFlags = (flags & EnumMessagesFlag.IsSequentialScanningHint) != 0 ? MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading : MessagesParserFlag.None;
			await DisposableAsync.Using(await ctx.Reader.CreateParser(
				new CreateParserParams(positionToContinueAsync, null, parserFlags, direction)), async parser =>
			{
				for (; ; )
				{
					ctx.Cancellation.ThrowIfCancellationRequested();
					var m = await parser.ReadNext();
					if (m == null)
						break;
					if (!callback(m))
						break;
				}
			});
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
			if (e != null)
				task.SetException(e);
			else
				task.SetResult(0);
		}

		readonly TaskCompletionSource<int> task = new TaskCompletionSource<int>();
		readonly long startFrom;
		readonly EnumMessagesFlag flags;
		readonly Func<IMessage, bool> callback;
		readonly MessagesParserDirection direction;

		long positionToContinueAsync;
	};
}
