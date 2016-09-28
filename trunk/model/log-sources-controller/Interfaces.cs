using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogSourcesController
	{
		ILogSource CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams);
		Task DeleteAllLogsAndPreprocessings();
	};
}
