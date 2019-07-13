using LogJoint.UI.Presenters;
using System;
using MPD = LogJoint.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog
{
	class Extension : MPD.IExtension
	{
		readonly IPostprocessorsRegistry postprocessorsRegistry;
		readonly Func<IView> viewFactory;
		readonly IClipboardAccess clipboardAccess;
		IPresenter presenter;

		public Extension(
			IPostprocessorsRegistry postprocessorsRegistry,
			Func<IView> viewFactory,
			IClipboardAccess clipboardAccess
		)
		{
			this.postprocessorsRegistry = postprocessorsRegistry;
			this.viewFactory = viewFactory;
			this.clipboardAccess = clipboardAccess;
		}

		MPD.IMessageContentPresenter MPD.IExtension.CreateContentPresenter(MPD.ContentPresenterParams @params)
		{
			IPresenter result = null;
			if (@params.Message.GetLogSource()?.Provider?.Factory ==
				postprocessorsRegistry.WiresharkPdml.LogProviderFactory)
			{
				if (presenter == null)
				{
					presenter = new Presenter(
						viewFactory(),
						@params.ChangeNotification,
						clipboardAccess
					);
				}
				result = presenter;
				result.SetMessage(@params.Message);
			}
			return result;
		}
	};
};