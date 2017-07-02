using LogJoint.Analytics.Messaging.Analisys;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LogJoint.Analytics.Correlation
{
	public class SolverFoundationSolver: ISolver
	{
		readonly static Lazy<SolverPluginsLoader> solverPluginsLoader = new Lazy<SolverPluginsLoader>(() => new SolverPluginsLoader(), true);

		public SolverFoundationSolver()
		{
			solverPluginsLoader.Value.GetHashCode();
			SolverContext.GetContext();
		}
	}
}
