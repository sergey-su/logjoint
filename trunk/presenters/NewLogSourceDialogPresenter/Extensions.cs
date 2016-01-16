using System;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
	public static class Extensions
	{
		public static void ShowTheDialog(this IPresenter presenter, ILogProviderFactory selectedFactory)
		{
			presenter.ShowTheDialog(LogProviderFactoryRegistry.ToString(selectedFactory));
		}
	};
};