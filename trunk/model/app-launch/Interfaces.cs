using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.AppLaunch
{
    public interface ICommandLineHandler
    {
        Task HandleCommandLineArgs(string[] appArgs, CommandLineEventArgs eventArgs);
        void RegisterCommandHandler(IBatchCommandHandler handler);
    };

    public interface ILaunchUrlParser
    {
        bool TryParseLaunchUri(Uri uri, out LaunchUriData data);
        string ProtocolName { get; }
    }

    public class LaunchUriData
    {
        public string SingleLogUri;
        public string WorkspaceUri;
    };

    public class CommandLineEventArgs : EventArgs
    {
        public bool ContinueExecution;
    };

    public interface IBatchCommandHandler
    {
        string[] SupportedCommands { get; }
        Task Run(XElement commandConfig);
    };
}
