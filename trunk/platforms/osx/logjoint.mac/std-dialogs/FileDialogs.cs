using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using LogJoint.UI.Presenters;
using AppKit;

namespace LogJoint.UI
{
	public class FileDialogs : IFileDialogs
	{
		string [] IFileDialogs.OpenFileDialog (OpenFileDialogParams prms)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = prms.CanChooseFiles;
			dlg.AllowsMultipleSelection = prms.AllowsMultipleSelection;
			dlg.CanChooseDirectories = prms.CanChooseDirectories;

			if (dlg.RunModal () == 1) 
			{
				return dlg.Urls.Select(u => u.Path).Where(p => p != null).ToArray();
			}

			return null;
		}

		string IFileDialogs.SaveFileDialog(SaveFileDialogParams p)
		{
			return SaveFileDialog(p);
		}

		async Task IFileDialogs.SaveOrDownloadFile (
			Func<Stream, Task> saver, SaveFileDialogParams p)
		{
			string fname = SaveFileDialog (p);
			if (fname != null) {
				using (var fs = new FileStream (fname, FileMode.Create))
					await saver (fs);
			}
		}

		string SaveFileDialog (SaveFileDialogParams p)
		{
			var dlg = new NSSavePanel ();
			dlg.Title = p.Title ?? "Save";
			if (p.SuggestedFileName != null)
				dlg.NameFieldStringValue = p.SuggestedFileName;
			if (dlg.RunModal () == 1) {
				return dlg.Url.Path;
			}
			return null;
		}
	}
}

