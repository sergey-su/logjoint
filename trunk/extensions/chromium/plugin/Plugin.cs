using System;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IModel model, UI.Presenters.IPresentation presentation)
		{
			LogJoint.Chromium.Factory.Create(model);
			LogJoint.Chromium.UI.Presenters.Factory.Create(presentation);
		}
	}
}
