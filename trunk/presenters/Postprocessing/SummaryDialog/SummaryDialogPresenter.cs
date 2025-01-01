using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace LogJoint.UI.Presenters.Postprocessing.SummaryDialog
{
    internal class Presenter : IPresenter, IViewModel
    {
        readonly IChangeNotification changeNotification;
        readonly IPresentersFacade facade;
        bool isEnabled;
        bool isVisible;
        IReadOnlyList<ViewItem> items = new ViewItem[0];

        public Presenter(IChangeNotification changeNotification, IPresentersFacade facade)
        {
            this.changeNotification = changeNotification;
            this.facade = facade;
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.IsVisible => isVisible;

        IReadOnlyList<ViewItem> IViewModel.Items => items;

        void IViewModel.OnCancel()
        {
            isVisible = false;
            items = new ViewItem[0];
            changeNotification.Post();
        }

        void IViewModel.OnLinkClicked(object linkData)
        {
            if (linkData is IBookmark bookmark)
            {
                facade.ShowMessage(bookmark, BookmarkNavigationOptions.EnablePopups);
            }
            else if (linkData is ILogSource logSource)
            {
                facade.ShowLogSource(logSource);
            }
        }

        bool IPresenter.IsEnabled => isEnabled;

        void IPresenter.Enable()
        {
            isEnabled = true;
        }

        void IPresenter.ShowDialog(IEnumerable<(ILogSource logSource, IStructuredPostprocessorRunSummary summary)> summaries)
        {
            items = summaries.SelectMany(s =>
                Enumerable.Union(
                    new[]
                    {
                        new ViewItem()
                        {
                            Kind = ViewItem.ItemKind.LogSource,
                            Text = s.logSource.DisplayName,
                            LinkData = s.logSource,
                        }
                    },
                    s.summary.Entries.Select(e => new ViewItem()
                    {
                        Kind = ViewItem.ItemKind.Issue,
                        Text = e.text,
                        LinkData = e.bookmark
                    })
                )
            ).ToArray();
            isVisible = true;
            changeNotification.Post();
        }
    }
}
