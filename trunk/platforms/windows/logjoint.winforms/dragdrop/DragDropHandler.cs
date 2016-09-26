using LogJoint.Preprocessing;
using LogJoint.UI.Presenters.MainForm;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint
{
	class DragDropHandler : IDragDropHandler
	{
		readonly ILogSourcesManager logSourcesManager;
		readonly ILogSourcesPreprocessingManager preprocessingManager;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;

		public DragDropHandler(
			ILogSourcesManager logSourcesManager,
			ILogSourcesPreprocessingManager preprocessingManager,
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.logSourcesManager = logSourcesManager;
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		bool ShouldAcceptDragDrop(IDataObject dataObject)
		{
			if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
				return true;
			else if (UrlDragDropUtils.IsUriDataPresent(dataObject))
				return true;
			return false;
		}

		async void AcceptDragDrop(IDataObject dataObject, bool controlKeyHeld)
		{
			if (UrlDragDropUtils.IsUriDataPresent(dataObject))
			{
				if (controlKeyHeld)
					await ModelExtensions.DeleteAllLogsAndPreprocessings(logSourcesManager, preprocessingManager);
				var urls = UrlDragDropUtils.GetURLs(dataObject).ToArray();
				await preprocessingManager.Preprocess(
					urls.Select(url => preprocessingStepsFactory.CreateURLTypeDetectionStep(new PreprocessingStepParams(url))),
					urls.Length == 1 ? urls[0] : "Urls drag&drop"
				);
			}
			else if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
			{
				if (controlKeyHeld)
					await ModelExtensions.DeleteAllLogsAndPreprocessings(logSourcesManager, preprocessingManager);
				((dataObject.GetData(DataFormats.FileDrop) as string[]) ?? new string[0]).Select(file =>
					preprocessingManager.Preprocess(
						Enumerable.Repeat(preprocessingStepsFactory.CreateFormatDetectionStep(new PreprocessingStepParams(file)), 1),
						file
					)
				).ToArray();
			}
		}

		bool IDragDropHandler.ShouldAcceptDragDrop(object unkDataObject)
		{
			var dataObject = unkDataObject as IDataObject;
			if (dataObject != null)
				return ShouldAcceptDragDrop(dataObject);
			return false;
		}

		void IDragDropHandler.AcceptDragDrop(object unkDataObject, bool controlKeyHeld)
		{
			var dataObject = unkDataObject as IDataObject;
			if (dataObject != null)
				AcceptDragDrop(dataObject, controlKeyHeld);
		}
	};
}
