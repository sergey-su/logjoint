using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.ToastNotificationPresenter
{
	public class Presenter: IPresenter, IViewEvents
	{
		readonly IView view;
		readonly List<ItemData> items = new List<ItemData>();
		readonly IChangeNotification changeNotification;
		bool hasSuppressedNotifications;

		public Presenter(
			IView view,
			IChangeNotification changeNotification
		)
		{
			this.view = view;
			this.changeNotification = changeNotification;
			view?.SetEventsHandler(this);
		}

		void IPresenter.Register (IToastNotificationItem item)
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

		void IViewEvents.OnItemActionClicked (ViewItem item, string actionId)
		{
			ModifyItem(item, i => i.source.PerformAction(actionId));
		}

		void IViewEvents.OnItemSuppressButtonClicked (ViewItem item)
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
			var viewItems = 
				items
				.Where(i => i.source.IsActive && !i.suppressed)
				.Select(i => i.viewItem = new ViewItem() 
				{
					Contents = i.source.Contents,
					Progress = i.source.Progress,
					IsSuppressable = true,
				})
				.ToArray();
			view?.SetVisibility(viewItems.Length > 0);
			view?.Update(viewItems);
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
					if (SuppressedNotificationsChanged != null)
						SuppressedNotificationsChanged(this, EventArgs.Empty);
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
