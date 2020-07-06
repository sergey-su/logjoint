using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public class DetectedFormat
	{
		public readonly ILogProviderFactory Factory;
		public readonly IConnectionParams ConnectParams;
		public DetectedFormat(ILogProviderFactory fact, IConnectionParams cp)
		{
			Factory = fact;
			ConnectParams = cp;
		}
	};

	public interface IFormatAutodetect
	{
		Task<DetectedFormat> DetectFormat(string fileName, string loggableName, CancellationToken cancellation, IFormatAutodetectionProgress progress);
		IFormatAutodetect Clone();
	};

	public interface IFormatAutodetectionProgress
	{
		void Trying(ILogProviderFactory factory);
	};
}
