using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
    internal interface ILogSourcesManagerInternal : ILogSourcesManager
    {
        void Add(ILogSource ls);
        void Remove(ILogSource ls);

        #region Single-threaded notifications
        void FireOnLogSourceAdded(ILogSource sender);
        void FireOnLogSourceRemoved(ILogSource sender);
        void OnTimegapsChanged(ILogSource logSource);
        void OnSourceVisibilityChanged(ILogSource logSource);
        void OnSourceTrackingChanged(ILogSource logSource);
        void OnSourceAnnotationChanged(ILogSource logSource);
        void OnSourceColorChanged(ILogSource logSource);
        void OnTimeOffsetChanged(ILogSource logSource);
        #endregion

        #region Notification fired from a unknown thread
        void OnSourceStatsChanged(ILogSource logSource, LogProviderStats value,
            LogProviderStats oldValue, LogProviderStatsFlag flags);
        #endregion
    };


    internal interface ILogSourceInternal : ILogSource, ILogProviderHost
    {
    };

    internal interface ILogSourceFactory
    {
        Task<ILogSourceInternal> CreateLogSource(
            ILogSourcesManagerInternal owner,
            int id,
            ILogProviderFactory providerFactory,
            IConnectionParams connectionParams
        );
    };
}
