using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
    /// <summary>
    /// Interface for a simple prompt dialog.
    /// </summary>
    public interface IPromptDialog
    {
        /// <summary>
        /// Opens a mocal dialog with given caption and prompt message
        /// allowing user to edit the string of text.
        /// </summary>
        /// <param name="caption">Short dialog's caption</param>
        /// <param name="prompt">Prompt text. Can be long.</param>
        /// <param name="defaultValue">Pre-filled value for the text.</param>
        /// <returns>Value of text string that the user entered.
        /// null if dialog was cancelled.</returns>
        string ExecuteDialog(string caption, string prompt, string defaultValue);

        /// <summary>
        /// Async version of ExecuteDialog.
        /// </summary>
        Task<string> ExecuteDialogAsync(string caption, string prompt, string defaultValue);
    };
};