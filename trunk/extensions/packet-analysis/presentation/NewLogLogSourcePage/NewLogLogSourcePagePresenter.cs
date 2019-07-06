using System;
using System.Linq;
using LogJoint.UI.Presenters.NewLogSourceDialog;
using LogJoint.Wireshark.Dpml;

namespace LogJoint.PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage
{
	public class Presenter : IPagePresenter
	{
		readonly IView view;
		readonly Preprocessing.IManager preprocessingManager;
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly ITShark tShark;

		public Presenter(
			IView view,
			Preprocessing.IManager preprocessingManager,
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			ITShark tShark
		)
		{
			this.view = view;
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.tShark = tShark;
		}

		void IPagePresenter.Apply()
		{
			string pcapFile = view.PcapFileNameValue.Trim();
			string keyFile = view.KeyFileNameValue.Trim();
			if (pcapFile == "")
				return;
			view.PcapFileNameValue = "";

			
			preprocessingManager.Preprocess(
				(new[] { pcapFile, keyFile })
					.Where(arg => !string.IsNullOrWhiteSpace(arg))
					.Select(arg => preprocessingStepsFactory.CreateLocationTypeDetectionStep(new Preprocessing.PreprocessingStepParams(arg))),
				"Processing selected file"
			);
		}

		void IPagePresenter.Activate()
		{
			view.SetError(tShark.IsAvailable ? null : "Can not decode pcap files. tshark is not installed on your system.");
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