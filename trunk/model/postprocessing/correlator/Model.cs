using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Messaging;
using LogJoint.Postprocessing.Messaging.Analisys;

namespace LogJoint.Postprocessing.Correlation
{
	class Model : IModel
	{
		readonly ITempFilesManager tempFiles;

		public Model(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		Task IModel.SavePostprocessorOutput(
			NodeId nodeId,
			Task<ILogPartToken> logPartTask,
			IEnumerableAsync<Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			throw new NotImplementedException();
		}
	}
}
