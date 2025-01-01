using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Options
{
    public static class PluginsInstallationOffer
    {
        public static void Init(
            Dialog.IPresenter optionsDialogPresenter,
            Plugins.IPageAvailability pluginsPageAvailability,
            Persistence.IStorageManager storageManager,
            MainForm.IPresenter mainFormPresenter,
            IAlertPopup popup
        )
        {
            async void handler(object s, EventArgs e)
            {
                mainFormPresenter.Loaded -= handler;
                if (pluginsPageAvailability.IsAvailable)
                {
                    bool showOffer = false;
                    var storageEntry = await storageManager.GetEntry("PluginsInstallationOffer");
                    await using (var section = await storageEntry.OpenXMLSection("state", Persistence.StorageSectionOpenFlag.ReadWrite))
                    {
                        if (section.Data.Root == null)
                        {
                            showOffer = true;
                            section.Data.Add(new XElement("root"));
                        }
                    }
                    if (showOffer)
                    {
                        await Task.Delay(1000);
                        string pluginsLocation = "LogJoint options dialog";
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            pluginsLocation = "menu LogJoint -> Preferences... -> Plug-ins";
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            pluginsLocation = "Options... -> Configuration... -> Plug-ins";
                        }
                        if (popup.ShowPopup(
                            "Plug-ins",
                            "LogJoint offers features via plug-ins. Do you want to choose plug-ins now?" + Environment.NewLine + Environment.NewLine +
                            "You can manage plug-ins any time in " + Environment.NewLine +
                            pluginsLocation,
                            AlertFlags.YesNoCancel
                        ) == AlertFlags.Yes)
                        {
                            optionsDialogPresenter.ShowDialog(Dialog.PageId.Plugins);
                        }
                    }
                }
            }

            mainFormPresenter.Loaded += handler;
        }
    }
}
