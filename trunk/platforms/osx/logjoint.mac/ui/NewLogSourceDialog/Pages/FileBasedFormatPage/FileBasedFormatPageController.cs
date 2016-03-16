using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace LogJoint.UI
{
	public partial class FileBasedFormatPageController : MonoMac.AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public FileBasedFormatPageController(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public FileBasedFormatPageController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public FileBasedFormatPageController()
			: base("FileBasedFormatPage", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new FileBasedFormatPage View
		{
			get
			{
				return (FileBasedFormatPage)base.View;
			}
		}
			
		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		object IView.ReadControlValue(ControlId id)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					return independentLogsModeButton.State == NSCellStateValue.On;
				case ControlId.RotatedLogModeButton:
					return rotatedLogModeButton.State == NSCellStateValue.On;
				case ControlId.FileSelector:
					return fileTextField.StringValue;
				case ControlId.FolderSelector:
					return folderTextField.StringValue;
			}
			return null;
		}

		void IView.WriteControlValue(ControlId id, object value)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					independentLogsModeButton.State = ((bool)value) ? NSCellStateValue.On : NSCellStateValue.Off;
					break;
				case ControlId.RotatedLogModeButton:
					rotatedLogModeButton.State = ((bool)value) ? NSCellStateValue.On : NSCellStateValue.Off;
					break;
				case ControlId.FileSelector:
					fileTextField.StringValue = (string)value;
					break;
				case ControlId.FolderSelector:
					folderTextField.StringValue = (string)value;
					break;
			}
		}

		void IView.SetEnabled(ControlId id, bool value)
		{
			switch (id)
			{
				case ControlId.IndependentLogsModeButton:
					independentLogsModeButton.Enabled = (bool)value;
					break;
				case ControlId.RotatedLogModeButton:
					rotatedLogModeButton.Enabled = (bool)value;
					break;
				case ControlId.FileSelector:
					fileTextField.Enabled = value;
					browseFileButton.Enabled = value;
					break;
				case ControlId.FolderSelector:
					folderTextField.Enabled = value;
					browseFolderButton.Enabled = value;
					break;
			}
		}

		string[] IView.ShowFilesSelectorDialog(string filters)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.AllowsMultipleSelection = true;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal () == 1) 
			{
				return dlg.Urls.Select(u => u.Path).Where(p => p != null).ToArray();
			}

			return null;
		}

		string IView.ShowFolderSelectorDialog()
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseDirectories = true;
			dlg.CanChooseFiles = false;

			if (dlg.RunModal () == 1) 
			{
				return dlg.Urls.Select(u => u.Path).FirstOrDefault();
			}

			return null;
		}

		object IView.PageView
		{
			get { return View; }
		}

		partial void OnModeSelectionChanged (NSObject sender)
		{
			eventsHandler.OnSelectedModeChanged();
		}

		partial void OnBrowseFileButtonClicked (NSObject sender)
		{
			eventsHandler.OnBrowseFilesButtonClicked();
		}

		partial void OnBrowseFolderButtonClicked (NSObject sender)
		{
			eventsHandler.OnBrowseFolderButtonClicked();
		}


	}
}

