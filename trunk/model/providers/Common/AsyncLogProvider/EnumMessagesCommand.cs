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
                ReadMessagesDirection.Backward : ReadMessagesDirection.Forward;
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
            if (direction == ReadMessagesDirection.Forward && startFrom >= ctx.Stats.PositionsRange.End)
                return true;
            if (direction == ReadMessagesDirection.Backward && startFrom <= ctx.Stats.PositionsRange.Begin)
                return true;

            bool finishedSynchroniously = false;
            var testRange = direction == ReadMessagesDirection.Forward ?
                cache.MessagesRange : cache.MessagesRange.ChangeDirection();
            if (testRange.IsInRange(startFrom))
            {
                foreach (var i in (direction == ReadMessagesDirection.Forward ? ctx.Cache.Messages.Forward(startFrom) : ctx.Cache.Messages.Reverse(startFrom)))
                {
                    finishedSynchroniously = !callback(i.Message);
                    if (finishedSynchroniously)
                        break;
                    ctx.Cancellation.ThrowIfCancellationRequested();
                    positionToContinueAsync = direction == ReadMessagesDirection.Forward ?
                        i.Message.EndPosition : i.Message.Position - 1;
                }
                if (!finishedSynchroniously)
                {
                    if (direction == ReadMessagesDirection.Backward)
                        // example: reading from position AvailableRange.Begin+1
                        finishedSynchroniously = ctx.Cache.MessagesRange.Begin == ctx.Stats.PositionsRange.Begin;
                    else if (direction == ReadMessagesDirection.Forward)
                        // example: reading from position AvailableRange.End-1
                        finishedSynchroniously = ctx.Cache.MessagesRange.End == ctx.Stats.PositionsRange.End;
                }
            }
            return finishedSynchroniously;
        }

        async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
        {
            var parserFlags = (flags & EnumMessagesFlag.IsSequentialScanningHint) != 0 ? ReadMessagesFlag.HintMassiveSequentialReading : ReadMessagesFlag.None;
            await foreach (PostprocessedMessage m in ctx.Reader.Read(
                new ReadMessagesParams(positionToContinueAsync, null, parserFlags, direction)))
            {
                ctx.Cancellation.ThrowIfCancellationRequested();
                ctx.Preemption.ThrowIfCancellationRequested();
                if (!callback(m.Message))
                    break;
            }
        }

        void IAsyncLogProviderCommandHandler.Complete(Exception? e)
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
        readonly ReadMessagesDirection direction;

        long positionToContinueAsync;
    };
}
