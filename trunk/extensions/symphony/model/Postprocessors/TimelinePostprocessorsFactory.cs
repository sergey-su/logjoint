using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using LogJoint.Postprocessing.Timeline;
using System.Xml;

namespace LogJoint.Symphony.Timeline
{
	public interface IPostprocessorsFactory
	{
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly static string typeId = PostprocessorIds.Timeline;
		readonly static string caption = PostprocessorIds.Timeline;
		readonly ITempFilesManager tempFiles;
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(
			ITempFilesManager tempFiles,
			Postprocessing.IModel postprocessing)
		{
			this.tempFiles = tempFiles;
			this.postprocessing = postprocessing;
		}

	};
}
