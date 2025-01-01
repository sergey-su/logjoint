using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
    public interface IShellOpen
    {
        void OpenInWebBrowser(Uri uri);
        void OpenFileBrowser(string filePath);
        /// <summary>
        /// Fire and forget file viewer launcher
        /// </summary>
        void OpenInTextEditor(string filePath);
        /// <summary>
        /// Awaitable file editor interaction
        /// </summary>
        Task EditFile(string filePath, CancellationToken cancel);
    }
}

