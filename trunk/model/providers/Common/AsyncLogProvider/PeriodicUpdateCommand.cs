using System;
using System.Threading.Tasks;

namespace LogJoint
{
	internal class PeriodicUpdateCommand : IAsyncLogProviderCommandHandler
	{
		public PeriodicUpdateCommand(IAsyncLogProvider owner)
		{
			this.owner = owner;
		}

		bool IAsyncLogProviderCommandHandler.RunSynchroniously(CommandContext ctx)
		{
			return false;
		}

		void IAsyncLogProviderCommandHandler.ContinueAsynchroniously(CommandContext ctx)
		{
			if (!owner.ResetPendingUpdateFlag())
				return;
			owner.UpdateAvailableTime(incrementalMode: true);
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
		}

		readonly IAsyncLogProvider owner;
	};
}