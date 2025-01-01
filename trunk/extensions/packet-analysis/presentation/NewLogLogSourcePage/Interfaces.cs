namespace LogJoint.PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage
{
    public interface IView
    {
        object PageView { get; }
        string PcapFileNameValue { get; set; }
        string KeyFileNameValue { get; set; }
        void SetError(string errorOrNull);
    };
};