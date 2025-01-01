using System;
using System.Linq;

namespace LogJoint.UI.Presenters.LogViewer
{
    class LoadedMessagesViewModeStrategy : IViewModeStrategy
    {
        readonly ILogSourcesManager logSourcesManager;
        readonly IChangeNotification changeNotification;
        readonly Func<bool> rawModeAllowed;
        readonly Func<bool> isRawMode;
        readonly EventHandler logSourceRemovedHandler;
        bool? manuallySetRawMode;

        public LoadedMessagesViewModeStrategy(
            ILogSourcesManager logSourcesManager,
            IChangeNotification changeNotification
        )
        {
            this.logSourcesManager = logSourcesManager;
            this.changeNotification = changeNotification;
            this.rawModeAllowed = Selectors.Create(
                () => logSourcesManager.VisibleItems,
                sources => sources.Any(s => s.Provider.Factory.ViewOptions.RawViewAllowed)
            );
            this.isRawMode = Selectors.Create(
                rawModeAllowed,
                () => manuallySetRawMode,
                () => logSourcesManager.VisibleItems,
                (allowed, set, sources) =>
                {
                    if (!allowed)
                        return false;
                    if (set != null)
                        return set.Value;
                    return sources.All(s => s.Provider.Factory.ViewOptions.PreferredView == PreferredViewMode.Raw);
                }
            );

            logSourceRemovedHandler = (s, e) =>
            {
                if (logSourcesManager.Items.Count == 0 && manuallySetRawMode.HasValue)
                {
                    manuallySetRawMode = null; // reset automatic mode when last source is gone
                    changeNotification.Post();
                }
            };

            logSourcesManager.OnLogSourceRemoved += logSourceRemovedHandler;
        }

        bool IViewModeStrategy.IsRawMessagesMode
        {
            get => isRawMode();
            set
            {
                if (rawModeAllowed())
                {
                    manuallySetRawMode = value;
                    changeNotification.Post();
                }
            }
        }

        bool IViewModeStrategy.IsRawMessagesModeAllowed => rawModeAllowed();

        void IDisposable.Dispose()
        {
            logSourcesManager.OnLogSourceRemoved -= logSourceRemovedHandler;
        }
    };
};