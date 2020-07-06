using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	/// <summary>
	/// Implements "GetDateBound" request that AsyncLogProvider supports.
	/// This class takes full use of provider's in-memory messages cache 
	/// trying to serve the request w/o reading the log (synchronously) when possible.
	/// It also caches its results in IDateBoundsCache. This cache usually gets hit when 
	/// user code performs binary search over the log's dates range. During such search
	/// user code requests dates at 1/2, 1/4, 3/4, 1/8, ... of full dates range.
	/// Binary search over full dates range is used during vertical scrolling.
	/// </summary>
	internal class GetDateBoundCommand : IAsyncLogProviderCommandHandler
	{
		public GetDateBoundCommand(DateTime date, bool getMessage, ValueBound bound, IDateBoundsCache dateBoundsCache)
		{
			this.date = date;
			this.bound = bound;
			this.messageRequested = getMessage;
			this.dateBoundsCache = dateBoundsCache;
		}

		public override string ToString()
		{
			return string.Format("{0}{1} {2:O}", bound, messageRequested ? "+m" : "", date);
		}

		public Task<DateBoundPositionResponseData> Task
		{
			get { return task.Task; }
		}

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			if (!messageRequested && (result = dateBoundsCache.Get(date)) != null)
				return true;

			if (ctx.Cache == null || ctx.Cache.Messages.Count == 0)
				return false;

			var cache = ctx.Cache;

			if ((date < ctx.Stats.AvailableTime.Begin && (bound == ValueBound.LowerReversed || bound == ValueBound.UpperReversed))
			 || date == ctx.Stats.AvailableTime.Begin && (bound == ValueBound.UpperReversed))
			{
				result = new DateBoundPositionResponseData()
				{
					IsBeforeBeginPosition = true,
					Position = ctx.Stats.PositionsRange.Begin - 1
				};
				return true;
			}
			if ((date >= ctx.Stats.AvailableTime.End && (bound == ValueBound.Lower || bound == ValueBound.Upper)))
			{
				result = new DateBoundPositionResponseData()
				{
					IsEndPosition = true,
					Position = ctx.Stats.PositionsRange.End
				};
				return true;
			}
			result = cache.Messages.GetDateBoundPosition(date, bound);
			if (result.Index == 0)
			{
				if (cache.MessagesRange.Begin != ctx.Stats.PositionsRange.Begin
					&& (bound == ValueBound.Lower || bound == ValueBound.Upper))
				{
					return false;
				}
			}
			else if (result.Index == -1)
			{
				if (cache.MessagesRange.Begin != ctx.Stats.PositionsRange.Begin)
				{
					return false;
				}
			}
			if (result.Index == cache.Messages.Count - 1)
			{
				if (cache.MessagesRange.End != ctx.Stats.PositionsRange.End
					&& (bound == ValueBound.LowerReversed || bound == ValueBound.UpperReversed))
				{
					return false;
				}
			}
			else if (result.Index == cache.Messages.Count)
			{
				if (cache.MessagesRange.End != ctx.Stats.PositionsRange.End)
				{
					return false;
				}
			}

			return true;
		}

		async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			result = new DateBoundPositionResponseData();

			result.Position = await PositionedMessagesUtils.LocateDateBound(ctx.Reader, date, bound, ctx.Cancellation);
			ctx.Tracer.Info("Position to return: {0}", result.Position);

			if (result.Position == ctx.Reader.EndPosition)
			{
				result.IsEndPosition = true;
				ctx.Tracer.Info("It is END position");
			}
			else if (result.Position == ctx.Reader.BeginPosition - 1)
			{
				result.IsBeforeBeginPosition = true;
				ctx.Tracer.Info("It is BEGIN-1 position");
			}
			else
			{
				ctx.Cancellation.ThrowIfCancellationRequested();
				if (messageRequested)
				{
					result.Message = await PositionedMessagesUtils.ReadNearestMessage(ctx.Reader, result.Position, MessagesParserFlag.HintMessageContentIsNotNeeed);
					ctx.Tracer.Info("Details to return: {0} at {1}", result.Message?.Time, result.Message?.Position);
				}
			}
			dateBoundsCache.Set(date, result);
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
			if (e != null)
				task.SetException(e);
			else
				task.SetResult(result);
		}

		readonly TaskCompletionSource<DateBoundPositionResponseData> task = new TaskCompletionSource<DateBoundPositionResponseData>();
		readonly DateTime date;
		readonly ValueBound bound;
		readonly bool messageRequested;
		readonly IDateBoundsCache dateBoundsCache;

		DateBoundPositionResponseData result;

		static DateBoundsCache dcache = new DateBoundsCache();
	};
}
