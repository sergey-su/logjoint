using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
    public interface IMessagesReaderExtension : IDisposable
    {
        void Attach(IMessagesReader reader);
        void OnAvailableBoundsUpdated(AvailableBoundsUpdateNotificationArgs param);
    };

    public struct AvailableBoundsUpdateNotificationArgs
    {
        public UpdateBoundsStatus Status;
        public bool IsIncrementalMode;
        public bool IsQuickFormatDetectionMode;
    };
}
