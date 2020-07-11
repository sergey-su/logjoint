using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogJoint.Settings;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.Wasm.UI.LogViewer
{
    public class ViewProxy : IView
    {
        public void SetComponent(IView component)
        {
            this.component = component;
            component?.SetViewModel(viewModel);
        }

        float IView.DisplayLinesPerPage => (component?.DisplayLinesPerPage).GetValueOrDefault(0);

        bool IView.HasInputFocus => (component?.HasInputFocus).GetValueOrDefault();

        string[] IViewFonts.AvailablePreferredFamilies => component?.AvailablePreferredFamilies ?? new string[0];

        KeyValuePair<Appearance.LogFontSize, int>[] IViewFonts.FontSizes => component?.FontSizes ?? new KeyValuePair<Appearance.LogFontSize, int>[0];

        object IView.GetContextMenuPopupData(int? viewLineIndex)
        {
            return component?.GetContextMenuPopupData(viewLineIndex);
        }

        void IView.HScrollToSelectedText(int charIndex)
        {
            component?.HScrollToSelectedText(charIndex);
        }

        void IView.PopupContextMenu(object contextMenuPopupData)
        {
            component?.PopupContextMenu(contextMenuPopupData);
        }

        void IView.ReceiveInputFocus()
        {
            component?.ReceiveInputFocus();
        }

        void IView.SetViewModel(IViewModel value)
        {
            viewModel = value;
        }

        public IViewModel viewModel;
        IView component;
    }
}
