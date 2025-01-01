using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint
{
    // Controls the visibility of the given modal dialog asynchrounously
    // without blocking the main thread.
    internal class ModalDialogController
    {
        readonly Form dialog;
        bool requiredVisibility;
        bool shown;

        public ModalDialogController(Form dialog)
        {
            this.dialog = dialog;
        }

        public void SetVisibility(bool value)
        {
            requiredVisibility = value;
            if (requiredVisibility != shown)
            {
                if (requiredVisibility)
                {
                    SynchronizationContext.Current.Post(_ => Actuate(), null);
                }
                else
                {
                    Actuate();
                }
            }
        }

        private void Actuate()
        {
            if (requiredVisibility == shown || dialog.IsDisposed)
            {
                return;
            }
            shown = requiredVisibility;
            if (requiredVisibility)
                dialog.ShowDialog();
            else
                dialog.Hide();
        }
    }
}
