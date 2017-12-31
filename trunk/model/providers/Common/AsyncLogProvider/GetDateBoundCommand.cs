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
		public GetDateBoundCommand(DateTime date, bool getDate, ListUtils.ValueBound bound, IDateBoundsCache dateBoundsCache)
		{
			this.date = date;
			this.bound = bound;
			this.userNeedsDate = getDate;
			this.dateBoundsCache = dateBoundsCache;
		}

		public Task<DateBoundPositionResponseData> Task
		{
			get { return task.Task; }
		}

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			if (!userNeedsDate && (result = dateBoundsCache.Get(date)) != null)
				return true;

			if (ctx.Cache == null || ctx.Cache.Messages.Count == 0)
				return false;

			var cache = ctx.Cache;

			if ((date < ctx.Stats.AvailableTime.Begin && (bound == ListUtils.ValueBound.LowerReversed || bound == ListUtils.ValueBound.UpperReversed))
			 || date == ctx.Stats.AvailableTime.Begin && (bound == ListUtils.ValueBound.UpperReversed))
			{
				result = new DateBoundPositionResponseData()
				{
					IsBeforeBeginPosition = true,
					Position = ctx.Stats.PositionsRange.Begin - 1
				};
				return true;
			}
			if ((date >= ctx.Stats.AvailableTime.End && (bound == ListUtils.ValueBound.Lower || bound == ListUtils.ValueBound.Upper)))
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
					&& (bound == ListUtils.ValueBound.Lower || bound == ListUtils.ValueBound.Upper))
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
					&& (bound == ListUtils.ValueBound.LowerReversed || bound == ListUtils.ValueBound.UpperReversed))
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

		void IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			result = new DateBoundPositionResponseData();

			result.Position = PositionedMessagesUtils.LocateDateBound(ctx.Reader, date, bound, ctx.Cancellation);
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
				if (userNeedsDate)
				{
					result.Date = PositionedMessagesUtils.ReadNearestMessageTimestamp(ctx.Reader, result.Position);
					ctx.Tracer.Info("Date to return: {0}", result.Date);
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
		readonly ListUtils.ValueBound bound;
		readonly bool userNeedsDate;
		readonly IDateBoundsCache dateBoundsCache;

		DateBoundPositionResponseData result;

		static DateBoundsCache dcache = new DateBoundsCache();
	};
}
