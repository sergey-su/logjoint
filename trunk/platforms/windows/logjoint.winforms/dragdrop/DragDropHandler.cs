using LogJoint.Preprocessing;
using LogJoint.UI.Presenters.MainForm;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint
{
	class DragDropHandler : IDragDropHandler
	{
		readonly ILogSourcesPreprocessingManager preprocessingManager;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly IModel model;

		public DragDropHandler(
			ILogSourcesPreprocessingManager preprocessingManager,
			IPreprocessingStepsFactory preprocessingStepsFactory,
			IModel model)
		{
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.model = model;
		}

		bool ShouldAcceptDragDrop(IDataObject dataObject)
		{
			if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
				return true;
			else if (UrlDragDropUtils.IsUriDataPresent(dataObject))
				return true;
			return false;
		}

		void AcceptDragDrop(IDataObject dataObject, bool controlKeyHeld)
		{
			if (UrlDragDropUtils.IsUriDataPresent(dataObject))
			{
				if (controlKeyHeld)
					DeleteExistingLogs();
				var urls = UrlDragDropUtils.GetURLs(dataObject).ToArray();
				preprocessingManager.Preprocess(
					urls.Select(url => preprocessingStepsFactory.CreateURLTypeDetectionStep(new PreprocessingStepParams(url))),
					urls.Length == 1 ? urls[0] : "Urls drag&drop"
				);
			}
			else if (dataObject.GetDataPresent(DataFormats.FileDrop, false))
			{
				if (controlKeyHeld)
					DeleteExistingLogs();
				foreach (var file in (dataObject.GetData(DataFormats.FileDrop) as string[]))
					preprocessingManager.Preprocess(
						Enumerable.Repeat(preprocessingStepsFactory.CreateFormatDetectionStep(new PreprocessingStepParams(file)), 1),
						file
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

		void IDragDropHandler.AcceptDragDrop(object unkDataObject, bool controlKeyHeld)
		{
			var dataObject = unkDataObject as IDataObject;
			if (dataObject != null)
				AcceptDragDrop(dataObject, controlKeyHeld);
		}

		void DeleteExistingLogs()
		{
			model.DeleteLogs(model.SourcesManager.Items.Where(s => !s.IsDisposed).ToArray());
			model.DeletePreprocessings(model.LogSourcesPreprocessingManager.Items.Where(s => !s.IsDisposed).ToArray());
		}
	};
}
