using LogJoint.UI.Presenters.MainForm;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint
{
	class DragDropHandler : IDragDropHandler
	{
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessingManager;
		readonly Preprocessing.IPreprocessingUserRequests userRequests;

		public DragDropHandler(
			Preprocessing.ILogSourcesPreprocessingManager preprocessingManager,
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

		bool IDragDropHandler.ShouldAcceptDragDrop(object unkDataObject)
		{
			var dataObject = unkDataObject as IDataObject;
			if (dataObject != null)
				return ShouldAcceptDragDrop(dataObject);
			return false;
		}

		void IDragDropHandler.AcceptDragDrop(object unkDataObject)
		{
			var dataObject = unkDataObject as IDataObject;
			if (dataObject != null)
				AcceptDragDrop(dataObject);
		}
	};
}
