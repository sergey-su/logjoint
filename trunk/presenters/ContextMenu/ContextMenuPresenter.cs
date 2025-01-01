using LogJoint.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.ContextMenu
{
    class Presenter : IContextMenu, IViewModel
    {
        readonly IChangeNotification changeNotification;
        ViewState viewState;

        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        IViewState IViewModel.ViewState => viewState;

        void IViewModel.OnClickOutside() => EnsureHidden();

        void IViewModel.OnItemClicked()
        {
            HandleSelection();
        }

        void IViewModel.OnItemHovered(int index)
        {
            viewState = viewState.WithSelectedItem(index);
            changeNotification.Post();
        }

        void IViewModel.OnItemUnhovered(int index)
        {
            viewState = viewState.WithSelectedItem(null);
            changeNotification.Post();
        }

        void IViewModel.OnKeyPressed(KeyCode key)
        {
            if (viewState == null || key == KeyCode.None)
            {
                return;
            }

            if (key == KeyCode.Down || key == KeyCode.Up)
            {
                viewState = viewState.WithSelectedItem((viewState.selectedItem.GetValueOrDefault(0)
                    + (key == KeyCode.Down ? 1 : -1) + viewState.items.Count) % viewState.items.Count);
                changeNotification.Post();
            }

            if (key == KeyCode.Enter)
            {
                HandleSelection();
            }
        }

        Task IContextMenu.ShowMenu(IReadOnlyList<ContextMenuItem> items, PointF? screenLocation)
        {
            EnsureHidden();
            if (items.Count == 0)
            {
                return Task.CompletedTask;
            }

            viewState = new ViewState()
            {
                items = items,
                displayItems = items.Select(i => i.Text).ToImmutableArray(),
                taskSource = new TaskCompletionSource(),
                selectedItem = 0,
                screenLocation = screenLocation,
            };
            changeNotification.Post();
            return viewState.taskSource.Task;
        }

        void HandleSelection()
        {
            Action action = viewState?.items?.ElementAtOrDefault(
                viewState.selectedItem.GetValueOrDefault(-1))?.Click;
            EnsureHidden();
            action?.Invoke();
        }

        void EnsureHidden()
        {
            if (viewState == null)
                return;
            viewState.taskSource.SetResult();
            viewState = null;
            changeNotification.Post();
        }

        class ViewState : IViewState
        {
            public IReadOnlyList<ContextMenuItem> items;
            public TaskCompletionSource taskSource;
            public IReadOnlyList<string> displayItems;
            public int? selectedItem;
            public PointF? screenLocation;


            IReadOnlyList<string> IViewState.Items => displayItems;

            int? IViewState.SelectedItem => selectedItem;

            PointF? IViewState.ScreenLocation => screenLocation;

            public ViewState WithSelectedItem(int? selectedItem)
            {
                return new ViewState()
                {
                    items = items,
                    taskSource = taskSource,
                    displayItems = displayItems,
                    selectedItem = selectedItem,
                    screenLocation = screenLocation
                };
            }
        };
    };
}