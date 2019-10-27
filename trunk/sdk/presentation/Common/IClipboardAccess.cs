
namespace LogJoint.UI.Presenters
{
	/// <summary>
	/// Provides access to system clipboard.
	/// </summary>
	public interface IClipboardAccess
	{
		/// <summary>
		/// Puts to the system clipboard the text in the plain-text format.
		/// </summary>
		void SetClipboard(string value);
		/// <summary>
		/// Puts to the system clipboard the text in two formats: plain-text and HTML.
		/// If any of the values is not specified, the value in corresponding format is
		/// not put to the clipboard.
		/// </summary>
		void SetClipboard(string plainText, string html);
	}
}

