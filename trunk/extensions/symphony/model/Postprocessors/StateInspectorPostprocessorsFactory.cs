using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.StateInspector;
using LogJoint.Analytics.StateInspector;

namespace LogJoint.Symphony.StateInspector
{
	public interface IPostprocessorsFactory
	{
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		private readonly ITempFilesManager tempFiles;
		private readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(
			ITempFilesManager tempFiles,
			Postprocessing.IModel postprocessing)
		{
			this.tempFiles = tempFiles;
			this.postprocessing = postprocessing;
		}
	};

}
