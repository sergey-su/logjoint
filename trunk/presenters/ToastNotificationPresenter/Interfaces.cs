using System;

namespace LogJoint.UI.Presenters.ToastNotificationPresenter
{
	public interface IPresenter
	{
		void Register(IToastNotificationItem item);
		bool HasSuppressedNotifications { get; }
		void UnsuppressNotifications();
		event EventHandler SuppressedNotificationsChanged;
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		void SetVisibility(bool visible);
		void Update(ViewItem[] items);
	};

	public class ViewItem
	{
		public string Contents;
		public double? Progress;
		public bool IsSuppressable;
	};

	public interface IViewEvents
	{
		void OnItemActionClicked(ViewItem item, string actionId);
		void OnItemSuppressButtonClicked(ViewItem item);
	};

	public interface IToastNotificationItem
	{
		bool IsActive { get; }
		string Contents { get; }
		double? Progress { get; }
		event EventHandler<ItemChangeEventArgs> Changed;
		void PerformAction(string actionId);
	};

	public class ItemChangeEventArgs
	{
		public bool IsUnsuppressingChange { get; private set; }

		public ItemChangeEventArgs(bool isUnsuppressingChange)
		{
			this.IsUnsuppressingChange = isUnsuppressingChange;
		}
	};
}
