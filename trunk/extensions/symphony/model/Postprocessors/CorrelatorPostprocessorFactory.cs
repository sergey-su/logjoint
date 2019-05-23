using System.Threading.Tasks;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.Correlator;
using LogJoint.Analytics.Correlation;
using M = LogJoint.Analytics.Messaging;
using System.Collections.Generic;
using LogJoint.Analytics.Messaging.Analisys;
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
			this.postprocessorsManager = ljModel.Postprocessing.PostprocessorsManager;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreatePostprocessor(IPostprocessorsRegistry postprocessorsRegistry)
		{
			return null;
		}
	}
}
