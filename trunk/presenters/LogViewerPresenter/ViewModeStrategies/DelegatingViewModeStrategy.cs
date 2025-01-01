using System;

namespace LogJoint.UI.Presenters.LogViewer
{
    class DelegatingViewModeStrategy : IViewModeStrategy
    {
        readonly IPresenterInternal referencePresenter;

        public DelegatingViewModeStrategy(
            IPresenterInternal referencePresenter
        )
        {
            this.referencePresenter = referencePresenter;
        }

        bool IViewModeStrategy.IsRawMessagesMode
        {
            get => referencePresenter.ShowRawMessages;
            set => referencePresenter.ShowRawMessages = value;
        }

        bool IViewModeStrategy.IsRawMessagesModeAllowed => referencePresenter.RawViewAllowed;

        void IDisposable.Dispose()
        {
        }
    };
};
