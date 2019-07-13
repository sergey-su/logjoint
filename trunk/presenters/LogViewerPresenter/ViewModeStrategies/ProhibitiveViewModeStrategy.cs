using System;

namespace LogJoint.UI.Presenters.LogViewer
{
	class ProhibitiveViewModeStrategy : IViewModeStrategy
	{
		bool IViewModeStrategy.IsRawMessagesMode
		{
			get => false;
			set {}
		}

		bool IViewModeStrategy.IsRawMessagesModeAllowed => false;

		void IDisposable.Dispose()
		{
		}
	};
};