using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.StateInspector
{
	public static class StateInspectorOutputExtensions
	{
		public static ILogSource GetPrimarySource(this IInspectedObject obj)
		{
			if (obj.CreationEvent != null)
				return obj.CreationEvent.Output.LogSource;
			var firstHistoryEvt = obj.StateChangeHistory.Select(h => h.Output.LogSource).FirstOrDefault();
			if (firstHistoryEvt != null)
				return firstHistoryEvt;
			return obj.Owner.Outputs.Select(x => x.LogSource).FirstOrDefault();
		}
	};
}
