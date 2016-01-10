using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.MRU;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
	public class Presenter : 
		IPresenter,
		IDialogViewEvents
	{
		readonly IView view;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		readonly IPagePresentersRegistry registry;
		readonly IRecentlyUsedEntities mru;
		readonly Func<IPagePresenter> formatDetectionPageFactory;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly FormatsWizard.IPresenter formatsWizardPresenter;
		IDialogView dialog;
		LogTypeEntry current;

		public Presenter(
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IPagePresentersRegistry registry,
			IRecentlyUsedEntities mru,
			IView view,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			Func<IPagePresenter> formatDetectionPageFactory,
			FormatsWizard.IPresenter formatsWizardPresenter
		)
		{
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.registry = registry;
			this.mru = mru;
			this.view = view;
			this.formatDetectionPageFactory = formatDetectionPageFactory;
			this.userDefinedFormatsManager = userDefinedFormatsManager;
			this.formatsWizardPresenter = formatsWizardPresenter;
		}

		void IPresenter.ShowTheDialog(ILogProviderFactory selectedFactory)
		{
			if (dialog == null)
				dialog = view.CreateDialog(this);
			UpdateList(selectedFactory);
			dialog.ShowModal();
		}

		IPagePresentersRegistry IPresenter.PagesRegistry { get { return registry; } }

		void IDialogViewEvents.OnSelectedIndexChanged()
		{
			LogTypeEntry tmp = GetSelected();
			SetCurrent(tmp);
		}

		void IDialogViewEvents.OnOKButtonClicked()
		{
			if (Apply())
				dialog.EndModal();
		}

		void IDialogViewEvents.OnApplyButtonClicked()
		{
			Apply();
		}

		void IDialogViewEvents.OnManageFormatsButtonClicked()
		{
			formatsWizardPresenter.ShowDialog();
			if (userDefinedFormatsManager.ReloadFactories() > 0)
			{
				UpdateList(null);
			}
		}

		#region Implementation


		void UpdateList(ILogProviderFactory selectedFactory)
		{
			object oldSelection = current != null ? current.GetIdentityObject() : null;
			if (selectedFactory != null)
				oldSelection = selectedFactory;
			SetCurrent(null);

			var items = new List<LogTypeEntry>();
			items.Add(new AutodetectedLogTypeEntry() { formatDetectionPageFactory = this.formatDetectionPageFactory });
			foreach (ILogProviderFactory fact in mru.SortFactoriesMoreRecentFirst(logProviderFactoryRegistry.Items))
			{
				FixedLogTypeEntry entry = new FixedLogTypeEntry();
				entry.Factory = fact;
				entry.UIsRegistry = registry;
				items.Add(entry);
			}

			int newSelectedIdx = 0;
			if (oldSelection != null)
			{
				for (int i = 0; i < items.Count; ++i)
				{
					if (items[i].GetIdentityObject() == oldSelection)
					{
						newSelectedIdx = i;
						break;
					}
				}
			}

			dialog.SetList(items.OfType<IViewListItem>().ToArray(), newSelectedIdx);
		}

		LogTypeEntry Get(int idx)
		{
			return dialog.GetItem(idx) as LogTypeEntry;
		}

		LogTypeEntry GetSelected()
		{
			var selectedIndex = dialog.SelectedIndex;
			if (selectedIndex >= 0)
				return Get(selectedIndex);
			return null;
		}

		void SetCurrent(LogTypeEntry entry)
		{
			LogTypeEntry tmp = entry;

			if (tmp == current)
				return;

			if (current != null)
			{
				if (current.UI != null)
				{
					dialog.DetachPageView(current.UI.View);
					current.UI.Deactivate();
				}
			}
			current = tmp;
			if (current != null)
			{
				dialog.SetFormatControls(current.ToString(), current.GetDescription());
				var ui = current.UI;
				if (current.UI == null)
				{
					ui = current.UI = current.CreateUI();
				}
				if (current.UI != null)
				{
					dialog.AttachPageView(ui.View);
					current.UI.Activate();
				}
			}
		}

		bool Apply()
		{
			// todo: handle errors
			if (current.UI != null)
				current.UI.Apply();
			return true;
		}

		#endregion


		abstract class LogTypeEntry : IViewListItem, IDisposable
		{
			public IPagePresenter UI;

			public abstract object GetIdentityObject();
			public abstract string GetDescription();
			public abstract IPagePresenter CreateUI();

			public void Dispose()
			{
				if (UI != null)
					UI.Dispose();
			}
		};

		class FixedLogTypeEntry : LogTypeEntry
		{
			public override string ToString() { return LogProviderFactoryRegistry.ToString(Factory); }

			public override object GetIdentityObject() { return Factory; }

			public override string GetDescription() { return Factory.FormatDescription; }

			public override IPagePresenter CreateUI() { return UIsRegistry.CreatePagePresenter(Factory); }

			public IPagePresentersRegistry UIsRegistry;
			public ILogProviderFactory Factory;
		};

		class AutodetectedLogTypeEntry : LogTypeEntry
		{
			public Func<IPagePresenter> formatDetectionPageFactory;

			public override string ToString() { return name; }

			public override object GetIdentityObject() { return name; }

			public override string GetDescription() { return "Pick a file or URL and LogJoint will detect log format by trying all known formats"; }

			public override IPagePresenter CreateUI()
			{ return formatDetectionPageFactory(); }

			private static string name = "Any known log format";
		};
	};
};