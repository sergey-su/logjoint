using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Settings
{
    public interface IDebugAgentConfig
    {
        string AgentAddress { get; }

        string ConfigComment { get; }

        Task UpdateAddress(string value);

        Task Refresh();
    };

    internal class DebugAgentConfig : IDebugAgentConfig
    {
        private readonly IChangeNotification changeNotification;
        private readonly Persistence.IStorageManager storage;
        private string address = null;
        private string configComment = null;
        private Task sequence;

        public DebugAgentConfig(IChangeNotification changeNotification,
            Persistence.IStorageManager storage, string configComment)
        {
            this.changeNotification = changeNotification;
            this.storage = storage;
            this.sequence = LoadFromStorage();
            this.configComment = configComment;
        }


        string IDebugAgentConfig.AgentAddress => address;

        string IDebugAgentConfig.ConfigComment => configComment;

        Task IDebugAgentConfig.Refresh()
        {
            sequence = sequence.ContinueWith((state) => LoadFromStorage());
            return sequence;
        }

        Task IDebugAgentConfig.UpdateAddress(string value)
        {
            sequence = sequence.ContinueWith((state) => SetAndStoreIntoStorage(value ?? ""));
            return sequence;
        }

        async Task LoadFromStorage()
        {
            var storageEntry = await storage.GetEntry("debug-agent");
            await using var section = await storageEntry.OpenXMLSection("settings", Persistence.StorageSectionOpenFlag.ReadOnly);
            string address = section?.Data?.Root?.Attribute("address")?.Value ?? "";
            if (this.address != address)
            {
                this.address = address;
                changeNotification.Post();
            }
        }

        async Task SetAndStoreIntoStorage(string value)
        {
            if (value == this.address)
                return;
            this.address = value;
            changeNotification.Post();
            var storageEntry = await storage.GetEntry("debug-agent");
            await using var section = await storageEntry.OpenXMLSection("settings", Persistence.StorageSectionOpenFlag.ReadWrite);
            if (section.Data.Root == null)
                section.Data.Add(new XElement("settings"));
            section.Data.Root.SetAttributeValue("address", value);
        }
    }
}
