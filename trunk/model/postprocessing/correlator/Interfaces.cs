using LogJoint.Analytics.Correlation;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Correlator
{
	public interface IPostprocessorsFactory
	{
		void Init(IPostprocessorsManager postprocessorsManager);
		ILogSourcePostprocessor CreatePostprocessor();
	};
}
