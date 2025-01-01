using System;
using LogJoint.NLog;
using System.Text;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.FormatsWizard.NLogGenerationLogPage
{
    internal class Presenter : IPresenter, IViewEvents
    {
        readonly IView view;
        readonly IWizardScenarioHost host;

        public Presenter(
            IView view,
            IWizardScenarioHost host
        )
        {
            this.view = view;
            this.view.SetEventsHandler(this);
            this.host = host;
        }

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            return true;
        }

        object IWizardPagePresenter.ViewObject => view;


        void IPresenter.UpdateView(ImportNLogPage.ISelectedLayout layout, ImportLog importLog)
        {
            var layoutTextBoxValue = new StringBuilder();
            var linksOffsets = new Dictionary<string, int>();

            if (layout is ImportNLogPage.ISimpleLayout)
            {
                layoutTextBoxValue.Append(((ImportNLogPage.ISimpleLayout)layout).Value);
                linksOffsets.Add("", 0);
            }
            else if (layout is ImportNLogPage.ICSVLayout)
            {
                var csv = layout as ImportNLogPage.ICSVLayout;
                foreach (var col in csv.Params.ColumnLayouts)
                {
                    if (layoutTextBoxValue.Length > 0)
                        layoutTextBoxValue.Append("  ");
                    linksOffsets.Add(col.Key, layoutTextBoxValue.Length);
                    layoutTextBoxValue.Append(col.Value);
                }
            }
            else if (layout is ImportNLogPage.IJsonLayout)
            {
                Action<JsonParams.Layout> handleLayout = null;
                handleLayout = jsonLayout =>
                {
                    foreach (var attr in jsonLayout.Attrs.Values)
                    {
                        if (attr.SimpleLayout != null)
                        {
                            if (layoutTextBoxValue.Length > 0)
                                layoutTextBoxValue.Append("  ");
                            linksOffsets.Add(attr.Id, layoutTextBoxValue.Length);
                            layoutTextBoxValue.Append(attr.SimpleLayout);
                        }
                        else if (attr.JsonLayout != null)
                        {
                            handleLayout(attr.JsonLayout);
                        }
                    }
                };
                handleLayout(((ImportNLogPage.IJsonLayout)layout).Params.Root);
            }

            string headerLabelValue;
            IconType headerIcon;
            if (importLog.HasErrors)
            {
                headerLabelValue = "LogJoint can not import your NLog layout. Check messages below.";
                headerIcon = IconType.ErrorIcon;
            }
            else if (importLog.HasWarnings)
            {
                headerLabelValue = "LogJoint imported your NLog layout with warnings. Check messages below.";
                headerIcon = IconType.WarningIcon;
            }
            else
            {
                headerLabelValue = null;
                headerIcon = IconType.None;
            }

            var messagesList = new List<MessagesListItem>();

            foreach (var message in importLog.Messages)
            {
                var linkLabel = new MessagesListItem()
                {
                    Links = new List<Tuple<int, int, Action>>()
                };
                int linksBaseIdx;
                linksOffsets.TryGetValue(message.LayoutId ?? "", out linksBaseIdx);

                StringBuilder messageText = new StringBuilder();

                foreach (var fragment in message.Fragments)
                {
                    if (messageText.Length > 0)
                        messageText.Append(' ');
                    var layoutSliceFragment = fragment as NLog.ImportLog.Message.LayoutSliceLink;
                    if (layoutSliceFragment != null)
                    {
                        linkLabel.Links.Add(Tuple.Create(messageText.Length, layoutSliceFragment.Value.Length, (Action)(() =>
                        {
                            view.SelectLayoutTextRange(linksBaseIdx + layoutSliceFragment.LayoutSliceStart, layoutSliceFragment.LayoutSliceEnd - layoutSliceFragment.LayoutSliceStart);
                        })));
                    }
                    messageText.Append(fragment.Value);
                }

                linkLabel.Text = messageText.ToString();

                if (message.Severity == NLog.ImportLog.MessageSeverity.Error)
                    linkLabel.Icon = IconType.ErrorIcon;
                else if (message.Severity == NLog.ImportLog.MessageSeverity.Warn)
                    linkLabel.Icon = IconType.WarningIcon;
                else
                    linkLabel.Icon = IconType.NeutralIcon;

                messagesList.Add(linkLabel);
            }

            view.Update(layoutTextBoxValue.ToString(), headerLabelValue, headerIcon, messagesList.ToArray());
        }
    };
};