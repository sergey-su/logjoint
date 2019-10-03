using System;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IModel model, UI.Presenters.IPresentation presentation)
		{
			LogJoint.Symphony.Factory.Create(model);
			LogJoint.Symphony.UI.Presenters.Factory.Create(presentation);
		}
	}
}
