using System;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IModel model, UI.Presenters.IPresentation presentation)
		{
			var modelObjects = Symphony.Factory.Create(model);
			Symphony.UI.Presenters.Factory.Create(model, modelObjects, presentation);
		}
	}
}
