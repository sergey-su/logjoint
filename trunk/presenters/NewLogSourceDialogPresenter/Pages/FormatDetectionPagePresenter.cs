using System;
using System.Linq;
using LogJoint.UI.Presenters.MainForm;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection
{
	public interface IView
	{
		object PageView { get; }
		string InputValue { get; set; }
	};

	public class Presenter : IPagePresenter
	{
		readonly IView view;
		readonly IManager preprocessingManager;
		readonly IStepsFactory preprocessingStepsFactory;

		public Presenter(
			IView view,
			IManager preprocessingManager,
			IStepsFactory preprocessingStepsFactory
		)
		{
			this.view = view;
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		void IPagePresenter.Apply()
		{
			string tmp = view.InputValue.Trim();
			if (tmp == "")
				return;
			view.InputValue = "";

			preprocessingManager.Preprocess(
				FileListUtils.ParseFileList(tmp).Select(arg => preprocessingStepsFactory.CreateLocationTypeDetectionStep(new PreprocessingStepParams(arg))),
				"Processing selected files"
			);
		}

		void IPagePresenter.Activate()
		{
		}

		void IPagePresenter.Deactivate()
		{
		}

		object IPagePresenter.View
		{
			get { return view.PageView; }
		}

		void IDisposable.Dispose()
		{
		}
	};
};