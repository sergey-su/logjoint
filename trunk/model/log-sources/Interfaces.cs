using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	internal interface ILogSourcesManagerInternal: ILogSourcesManager
	{
		List<ILogSource> Container { get; }

		#region Single-threaded notifications
		void FireOnLogSourceAdded(ILogSource sender);
		void FireOnLogSourceRemoved(ILogSource sender);
		void OnTimegapsChanged(ILogSource logSource);
		void OnSourceVisibilityChanged(ILogSource logSource);
		void OnSourceTrackingChanged(ILogSource logSource);
		void OnSourceAnnotationChanged(ILogSource logSource);
		void OnSourceColorChanged(ILogSource logSource);
		void OnTimeOffsetChanged(ILogSource logSource);
		void OnSourceStatsChanged(ILogSource logSource, LogProviderStatsFlag flags);
		#endregion
	};


	internal interface ILogSourceInternal : ILogSource, ILogProviderHost
	{
	};

	internal interface ILogSourceFactory
	{
		ILogSourceInternal CreateLogSource(
			ILogSourcesManagerInternal owner, 
			int id,
			ILogProviderFactory providerFactory, 
			IConnectionParams connectionParams
		);
	};
}
