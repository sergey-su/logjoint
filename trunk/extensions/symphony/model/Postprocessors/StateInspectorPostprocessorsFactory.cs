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
		readonly static string typeId = PostprocessorIds.StateInspector;
		readonly static string caption = PostprocessorIds.StateInspector;
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
