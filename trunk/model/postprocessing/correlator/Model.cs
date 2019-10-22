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
		readonly ILogPartTokenFactories logPartTokenFactories;
		readonly ISameNodeDetectionTokenFactories nodeDetectionTokenFactories;

		public Model(ITempFilesManager tempFiles,
			ILogPartTokenFactories logPartTokenFactories, ISameNodeDetectionTokenFactories nodeDetectionTokenFactories)
		{
			this.tempFiles = tempFiles;
			this.logPartTokenFactories = logPartTokenFactories;
			this.nodeDetectionTokenFactories = nodeDetectionTokenFactories;
		}

		Task IModel.SavePostprocessorOutput(
			Task<NodeId> nodeId,
			Task<ILogPartToken> logPartTask,
			IEnumerableAsync<Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			return CorrelatorPostprocessorOutput2.SerializePostprocessorOutput(
				nodeId,
				logPartTask,
				logPartTokenFactories,
				events,
				sameNodeDetectionTokenTask,
				nodeDetectionTokenFactories,
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.OutputFileName,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}
	}
}
