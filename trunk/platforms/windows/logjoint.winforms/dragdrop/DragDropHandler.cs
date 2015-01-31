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
				var urls = UrlDragDropUtils.GetURLs(dataObject).ToArray();
				preprocessingManager.Preprocess(
					urls.Select(url => new Preprocessing.URLTypeDetectionStep(url)),
					urls.Length == 1 ? urls[0] : "Urls drag&drop",
					userRequests
				);
			}
			else if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
			{
				foreach (var file in (dataObject.GetData(DataFormats.FileDrop) as string[]))
					preprocessingManager.Preprocess(
						Enumerable.Repeat(new Preprocessing.FormatDetectionStep(file), 1),
						file,
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
