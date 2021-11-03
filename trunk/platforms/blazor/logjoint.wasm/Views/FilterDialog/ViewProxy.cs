using LogJoint.Drawing;
using LogJoint.UI.Presenters.FilterDialog;
using System.Collections.Generic;

namespace LogJoint.Wasm.UI
{
    public class FilterDialogViewProxy : IView
    {
        DialogValues IView.GetData()
        {
            throw new System.NotImplementedException();
        }

        void IView.PutFocusOnNameEdit()
        {
            throw new System.NotImplementedException();
        }

        void IView.SetData(string title, KeyValuePair<string, Color?>[] actionComboBoxOptions, string[] typesOptions, DialogValues values)
        {
            throw new System.NotImplementedException();
        }

        void IView.SetEventsHandler(IViewEvents handler)
        {
        }

        void IView.SetNameEditProperties(NameEditBoxProperties props)
        {
            throw new System.NotImplementedException();
        }

        void IView.SetScopeItemChecked(int idx, bool checkedValue)
        {
            throw new System.NotImplementedException();
        }

        bool IView.ShowDialog()
        {
            throw new System.NotImplementedException();
        }
    }
}
