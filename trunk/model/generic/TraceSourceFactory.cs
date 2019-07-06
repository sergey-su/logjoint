using System.Linq;

namespace LogJoint
{
	public class TraceSourceFactory: ITraceSourceFactory
	{
		readonly TraceListener[] programmaticListeners;

		public TraceSourceFactory(TraceListener[] programmaticListeners = null)
		{
			this.programmaticListeners = programmaticListeners?.ToArray();
		}

		LJTraceSource ITraceSourceFactory.CreateTraceSource(string configName, string prefix)
		{
			return new LJTraceSource(configName, prefix, programmaticListeners);
		}
	}
}
