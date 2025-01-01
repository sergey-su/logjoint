using System.Linq;

namespace LogJoint
{
    public class TraceSourceFactory : ITraceSourceFactory
    {
        readonly TraceListener[] programmaticListeners;
        readonly bool removeDefaultTraceListener;

        public TraceSourceFactory(TraceListener[] programmaticListeners = null, bool removeDefaultTraceListener = false)
        {
            this.programmaticListeners = programmaticListeners?.ToArray();
            this.removeDefaultTraceListener = removeDefaultTraceListener;
        }

        LJTraceSource ITraceSourceFactory.CreateTraceSource(string configName, string prefix)
        {
            return new LJTraceSource(configName, prefix, programmaticListeners, removeDefaultTraceListener);
        }
    }
}
