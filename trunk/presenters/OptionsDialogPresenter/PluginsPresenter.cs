using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.Extensibility;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Presenters.Options.Plugins
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly IPluginsManagerInternal pluginsManager;
		readonly IView view;
		readonly IChainedChangeNotification changeNotification;
		readonly CancellationTokenSource fetchCancellation;
		PluginsListFetchingStatus fetchStatus = PluginsListFetchingStatus.Pending;
		IReadOnlyList<IPluginInfo> pluginsInfo = ImmutableArray.Create<IPluginInfo>();
		readonly Func<ImmutableArray<ListItem>> listItemsSelector;
		IPluginInfo selectedPlugin;
		readonly Func<ISelectedPluginData> selectedPluginDataSelector;
		readonly IPluginInstallationRequestsBuilder pluginInstallationRequestsBuilder;

		public Presenter(
			IView view,
			IPluginsManagerInternal pluginsManager,
			IChangeNotification changeNotification
		)
		{
			this.pluginsManager = pluginsManager;
			this.view = view;

			this.changeNotification = changeNotification.CreateChainedChangeNotification();
			this.fetchCancellation = new CancellationTokenSource();

			this.pluginInstallationRequestsBuilder = pluginsManager.CreatePluginInstallationRequestsBuilder();

			this.listItemsSelector = Selectors.Create(
				() => pluginInstallationRequestsBuilder.InstallationRequests,
				() => pluginsInfo,
				() => selectedPlugin,
				(requests, all, selected) =>
				{
					return ImmutableArray.CreateRange(all.Select(p => new ListItem(p, requests, selected?.Id)));
				}
			);

			this.selectedPluginDataSelector = Selectors.Create(
				listItemsSelector,
				list =>
				{
					var itemData = list.Select(i => i.MakeSelectedPluginData()).FirstOrDefault(i => i != null);
					return itemData ?? new SelectedPluginData
					{
						actionCaption = "Install",
						actionEnabled = false,
						caption = "",
						description = ""
					};
				}
			);

			view.SetViewModel(this);

			this.FetchPlugins();
		}

		bool IPresenter.Apply()
		{
			pluginInstallationRequestsBuilder.ApplyRequests();
			return true;
		}

		bool IPresenter.IsAvailable => pluginsManager.IsConfigured;

		void IDisposable.Dispose()
		{
			this.changeNotification.Dispose();
			this.fetchCancellation.Cancel();
		}

		void IViewModel.OnSelect(IPluginListItem item)
		{
			selectedPlugin = (item as ListItem)?.PluginInfo;
			changeNotification.Post();
		}

		void IViewModel.OnAction()
		{
			var item = listItemsSelector().FirstOrDefault(i => i.PluginInfo.Id == selectedPlugin?.Id);
			if (item == null)
				return;
			pluginInstallationRequestsBuilder.RequestInstallationState(item.PluginInfo, !item.IsInstalledOfInstallationRequested);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IReadOnlyList<IPluginListItem> IViewModel.ListItems => listItemsSelector();

		ISelectedPluginData IViewModel.SelectedPluginData => selectedPluginDataSelector();

		PluginsListFetchingStatus IViewModel.ListFetchingStatus => fetchStatus;

		async void FetchPlugins()
		{
			try
			{
				pluginsInfo = await pluginsManager.FetchAllPlugins(fetchCancellation.Token);
				fetchStatus = PluginsListFetchingStatus.Success;
				changeNotification.Post();
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception)
			{
				fetchStatus = PluginsListFetchingStatus.Failed;
				changeNotification.Post();
			}
		}

		class ListItem : IPluginListItem
		{
			readonly IPluginInfo pluginInfo;
			readonly string text;
			readonly bool selected;
			readonly bool isInstalledOfInstallationRequested;

			public ListItem(IPluginInfo pluginInfo, IReadOnlyDictionary<string, bool> installationRequests, string selectedId)
			{
				this.pluginInfo = pluginInfo;
				this.selected = selectedId == pluginInfo.Id;
				var (isInstalledOfInstallationRequested, statusString) =
					installationRequests.TryGetValue(pluginInfo.Id, out var currentRequest) ? 
						(currentRequest, currentRequest ? "(installation pending) " : "(uninstallation pending) ") :
						(pluginInfo.InstalledPluginManifest != null, pluginInfo.InstalledPluginManifest != null ? "(installed) " : "");
				this.text = $"{statusString}{pluginInfo.Name}";
				this.isInstalledOfInstallationRequested = isInstalledOfInstallationRequested;
			}

			public IPluginInfo PluginInfo => pluginInfo;
			public bool IsInstalledOfInstallationRequested => isInstalledOfInstallationRequested;

			string IPluginListItem.Text => text;
			string IListItem.Key => pluginInfo.Id;
			bool IListItem.IsSelected => selected;
			public override string ToString() => text;

			public ISelectedPluginData MakeSelectedPluginData()
			{
				if (!selected)
					return null;
				return new SelectedPluginData
				{
					caption = $"{pluginInfo.Name} ({(pluginInfo.Version).ToString(3)})",
					description = BuildDescription(),
					actionCaption = isInstalledOfInstallationRequested ? "Uninstall" : "Install",
					actionEnabled = true
				};
			}

			string BuildDescription()
			{
				var builder = new StringBuilder();
				builder.Append(pluginInfo.Description);
				if (pluginInfo != null)
				{
					if (pluginInfo.InstalledPluginManifest != null)
					{
						var deps = string.Join(", ", pluginInfo.Dependants.Select(d => d.Name));
						if (deps.Length > 0)
							builder.AppendFormat("{0}Other plug-in(s) require this one: {1}", Environment.NewLine, deps);
					}
					else
					{
						var deps = string.Join(", ", pluginInfo.Dependencies.Select(d => d.Name));
						if (deps.Length > 0)
							builder.AppendFormat("{0}Requires other plug-in(s): {1}", Environment.NewLine, deps);
					}
				}
				return builder.ToString();
			}
		};

		class SelectedPluginData : ISelectedPluginData
		{
			public string caption, description, actionCaption;
			public bool actionEnabled;

			string ISelectedPluginData.Caption => caption;
			string ISelectedPluginData.Description => description;
			(bool Enabled, string Caption) ISelectedPluginData.ActionButton => (actionEnabled, actionCaption);
		};
	};
};