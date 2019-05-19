using System.Threading.Tasks;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using M = LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using LogJoint.Postprocessing.Messaging.Analisys;
using System.Text;
using System;

namespace LogJoint.Symphony.Correlator
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreatePostprocessor(IPostprocessorsRegistry postprocessorsRegistry);
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly IModel ljModel;
		readonly ISynchronizationContext modelThreadSync;
		readonly IPostprocessorsManager postprocessorsManager;

		public PostprocessorsFactory(IModel ljModel)
		{
			this.ljModel = ljModel;
			this.modelThreadSync = ljModel.ModelThreadSynchronization;
			this.postprocessorsManager = ljModel.Postprocessing.Manager;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreatePostprocessor(IPostprocessorsRegistry postprocessorsRegistry)
		{
			return null;
		}
	}
}
