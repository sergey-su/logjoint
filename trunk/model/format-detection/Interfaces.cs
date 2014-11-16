using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		DetectedFormat DetectFormat(string fileName);
		IFormatAutodetect Clone();
	};
}
