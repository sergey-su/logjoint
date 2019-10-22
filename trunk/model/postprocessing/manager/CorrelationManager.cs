using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	interface ICorrelationManager // todo: move to interfaces
	{
		void Refresh();
	};

	class CorrelationManager
	{
		// Refresh() - detect change in postprocs, and spawn async correlation and update, do not run two async tasks in parallel
		// CurrentStatus -> reactive getter - combines: status of last run postprocs, own async task status
	}
}
