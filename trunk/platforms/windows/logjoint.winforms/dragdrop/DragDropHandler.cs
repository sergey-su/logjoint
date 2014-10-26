using System.Linq;
using System.Windows.Forms;

namespace LogJoint
{
	class DragDropHandler
	{
		readonly Preprocessing.LogSourcesPreprocessingManager preprocessingManager;
		readonly Preprocessing.IPreprocessingUserRequests userRequests;

		public DragDropHandler(
			Preprocessing.LogSourcesPreprocessingManager preprocessingManager,
			Preprocessing.IPreprocessingUserRequests userRequests)
		{
			this.preprocessingManager = preprocessingManager;
			this.userRequests = userRequests;
		}

		public bool ShouldAcceptDragDrop(IDataObject dataObject)
		{
			if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
				return true;
			else if (UrlDragDropUtils.IsUriDataPresent(dataObject))
				return true;
			return false;
		}

		public void AcceptDragDrop(IDataObject dataObject)
		{
			if (UrlDragDropUtils.IsUriDataPresent(dataObject))
			{
				preprocessingManager.Preprocess(
					UrlDragDropUtils.GetURLs(dataObject).Select(
						url => new Preprocessing.URLTypeDetectionStep(url)),
					userRequests
				);
			}
			else if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
			{
				foreach (var file in (dataObject.GetData(DataFormats.FileDrop) as string[]))
					preprocessingManager.Preprocess(
						Enumerable.Repeat(new Preprocessing.FormatDetectionStep(file), 1),
						userRequests
					);
			}
		}
	};
}
