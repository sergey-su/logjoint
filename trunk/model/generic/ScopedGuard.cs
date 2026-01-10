using System;

namespace LogJoint
{
    public class ScopedGuard : IDisposable
    {
        public ScopedGuard(Action? initilization, Action finalization)
        {
            if (initilization != null)
                initilization();
            this.finalization = finalization;
        }
        public ScopedGuard(Action finalization) : this(null, finalization)
        {
        }

        public void Dispose()
        {
            if (finalization != null)
            {
                finalization();
                finalization = null;
            }
        }

        Action? finalization;
    }
}
