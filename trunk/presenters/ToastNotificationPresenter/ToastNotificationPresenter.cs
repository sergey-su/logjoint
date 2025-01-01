using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.ToastNotificationPresenter
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly List<ItemData> items = new List<ItemData>();
        readonly IChangeNotification changeNotification;
        bool hasSuppressedNotifications;
        IReadOnlyList<ViewItem> viewItems = ImmutableArray<ViewItem>.Empty;

        public Presenter(
            IChangeNotification changeNotification
        )
        {
            this.changeNotification = changeNotification;
        }

        IViewModel IPresenter.ViewModel => this;

        void IPresenter.Register(IToastNotificationItem item)
        {
            items.Add(new ItemData()
            {
                source = item,
            });
            item.Changed += ItemChanged;
            UpdateView();
        }

        bool IPresenter.HasSuppressedNotifications
        {
            get { return hasSuppressedNotifications; }
        }

        void IPresenter.UnsuppressNotifications()
        {
            using (SuppressingOperationGuard())
                items.ForEach(i => { i.suppressed = false; });
            UpdateView();
        }

        public event EventHandler SuppressedNotificationsChanged;

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.Visible => viewItems.Count > 0;

        IReadOnlyList<ViewItem> IViewModel.Items => viewItems;

        void IViewModel.OnItemActionClicked(ViewItem item, string actionId)
        {
            ModifyItem(item, i => i.source.PerformAction(actionId));
        }

        void IViewModel.OnItemSuppressButtonClicked(ViewItem item)
        {
            ModifyItem(item, i =>
            {
                using (SuppressingOperationGuard())
                    i.suppressed = true;
                UpdateView();
            });
        }

        private void ItemChanged(object sender, ItemChangeEventArgs e)
        {
            ModifyItem(sender as IToastNotificationItem, i =>
            {
                using (SuppressingOperationGuard())
                    if (e.IsUnsuppressingChange)
                        i.suppressed = false;
                UpdateView();
            });
        }

        void UpdateView()
        {
            viewItems =
                items
                .Where(i => i.source.IsActive && !i.suppressed)
                .Select(i => i.viewItem = new ViewItem()
                {
                    Contents = i.source.Contents,
                    Progress = i.source.Progress,
                    IsSuppressable = true,
                })
                .ToImmutableArray();
            changeNotification.Post();
        }

        void ModifyItem(Predicate<ItemData> keyPredicate, Action<ItemData> action)
        {
            var i = items.FirstOrDefault(x => keyPredicate(x));
            if (i == null)
                return;
            action(i);
        }

        void ModifyItem(ViewItem key, Action<ItemData> action)
        {
            ModifyItem(x => key != null && x.viewItem == key, action);
        }

        void ModifyItem(IToastNotificationItem key, Action<ItemData> action)
        {
            ModifyItem(x => key != null && x.source == key, action);
        }

        IDisposable SuppressingOperationGuard()
        {
            return new ScopedGuard(() =>
            {
                var hasSuppressedNotifications = items.Any(i => i.source.IsActive && i.suppressed);
                if (this.hasSuppressedNotifications != hasSuppressedNotifications)
                {
                    this.hasSuppressedNotifications = hasSuppressedNotifications;
                    this.changeNotification.Post();
                    SuppressedNotificationsChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        class ItemData
        {
            public IToastNotificationItem source;
            public bool suppressed;
            public ViewItem viewItem;
        };
    }
}
