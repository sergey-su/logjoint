using System;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Preprocessing;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.PreprocessingUserInteractions
{
	public class PreprocessingInteractionsPresenter: IPresenter, IViewModel
	{
		private readonly IView view;
		private readonly ILogSourcesPreprocessingManager manager;
		private readonly IChangeNotification changeNotification;
		private readonly List<MutableItem> items = new List<MutableItem>();
		private int itemsRevision;
		private TaskCompletionSource<int> dialogDone;
		private readonly Func<DialogViewData> dialogViewData;
		private string selectedKey;

		public PreprocessingInteractionsPresenter(
			IView view,
			ILogSourcesPreprocessingManager manager,
			StatusReports.IPresenter statusReports,
			IChangeNotification changeNotification
		)
		{
			this.view = view;
			this.manager = manager;
			this.changeNotification = changeNotification;

			this.manager.PreprocessingYieldFailed += (sender, args) =>
			{
				statusReports.CreateNewStatusReport().ShowStatusPopup(
					args.LogSourcePreprocessing.DisplayName,
					"Failed to handle " + string.Join(", ", args.FailedProviders.Select(provider =>
						provider.Factory.GetUserFriendlyConnectionName(provider.ConnectionParams))),
					true
				);
			};

			this.manager.PreprocessingWillYieldProviders += (sender, arg) =>
			{
				if ((arg.LogSourcePreprocessing.Flags & PreprocessingOptions.Silent) != 0)
					return;
				if (arg.Providers.Count == 0)
				{
					statusReports.CreateNewStatusReport().ShowStatusPopup(
						arg.LogSourcePreprocessing.DisplayName,
						"No log of known format is detected",
						true
					);
				}
				else if (arg.Providers.Count > 1)
				{
					if (dialogDone == null)
						dialogDone = new TaskCompletionSource<int>();
					items.AddRange(arg.Providers.Select((p, i) => new MutableItem(arg, i)));
					++itemsRevision;
					arg.PostponeUntilCompleted(dialogDone.Task);
					changeNotification.Post();
				}
			};

			this.dialogViewData = Selectors.Create(
				() => itemsRevision,
				() => selectedKey,
				(rev, selectedKey) =>
				{
					if (items.Count == 0)
						return null;
					return new DialogViewData
					{
						Title = "Select logs to load",
						Items = ImmutableArray.CreateRange(items.Select(item => new ImmutableItem
						{
						 	Title = item.title,
							IsChecked = item.isChecked,
							Key = item.key,
							IsSelected = item.key == selectedKey,
						}))
					};
				}
			);

			this.view.SetViewModel(this);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		DialogViewData IViewModel.DialogData => dialogViewData();

		void IViewModel.OnCheck(IDialogItem item, bool value)
		{
			foreach (var i in items)
			{
				if (i.key == item.Key)
				{
					i.isChecked = value;
					++itemsRevision;
					changeNotification.Post();
					break;
				}
			}
		}

		void IViewModel.OnCheckAll(bool value)
		{
			foreach (var i in items)
				i.isChecked = value;
			++itemsRevision;
			changeNotification.Post();
		}

		void IViewModel.OnSelect(IDialogItem item)
		{
			selectedKey = item?.Key;
			changeNotification.Post();
		}

		void IViewModel.OnCloseDialog(bool accept)
		{
			items.ForEach(i => i.eventArg.SetIsAllowed(i.idx, accept && i.isChecked));
			items.Clear();
			itemsRevision++;
			dialogDone?.TrySetResult(0);
			dialogDone = null;
			changeNotification.Post();
		}

		class MutableItem
		{
			public readonly string key;
			public readonly LogSourcePreprocessingWillYieldEventArg eventArg;
			public readonly int idx;
			public readonly string title;
			public bool isChecked;
			
			public MutableItem(LogSourcePreprocessingWillYieldEventArg eventArg, int i)
			{
				this.eventArg = eventArg;
				this.idx = i;
				this.isChecked = eventArg.IsAllowed(i);
				var p = eventArg.Providers[i];
				this.title = $"{p.Factory.CompanyName}\\{p.Factory.FormatName}: {p.DisplayName}";
				this.key = $"{eventArg.GetHashCode()}/{i}";
			}
		};

		class ImmutableItem : IDialogItem
		{
			public string Title { get; internal set; }
			public bool IsChecked { get; internal set; }
			public string Key { get; internal set; }
			public bool IsSelected { get; internal set; }
		};
	};
}
