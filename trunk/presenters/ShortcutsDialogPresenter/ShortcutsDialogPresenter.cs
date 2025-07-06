namespace LogJoint.UI.Presenters.ShortcutsDialog
{
    internal class Presenter : IViewModel, IPresenter
    {
        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.IsVisible => isVisible;

        void IViewModel.OnCloseRequested()
        {
            if (isVisible)
            {
                isVisible = false;
                changeNotification.Post();
            }
        }

        void IPresenter.ShowDialog()
        {
            if (!isVisible)
            {
                isVisible = true;
                changeNotification.Post();
            }
        }

        private readonly IChangeNotification changeNotification;
        private bool isVisible;
    }
}
