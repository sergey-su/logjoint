
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage;

namespace LogJoint.PacketAnalysis.UI
{
	public partial class WiresharkPageAdapter : AppKit.NSViewController, IView
	{
		public WiresharkPageAdapter(NSBundle bundle)
			: base("WiresharkPage", bundle)
		{
		}

		object IView.PageView
		{
			get { return View; }
		}

		string IView.PcapFileNameValue
		{
			get => fileNameTextField.StringValue;
			set => fileNameTextField.StringValue = value;
		}
		string IView.KeyFileNameValue
		{
			get => keyTextField.StringValue;
			set => keyTextField.StringValue = value;
		}
		void IView.SetError(string errorOrNull)
		{
			errorLabel.Hidden = string.IsNullOrEmpty(errorOrNull);
			container.Hidden = !string.IsNullOrEmpty(errorOrNull);
			errorLabel.StringValue = errorOrNull ?? "";
		}

		partial void OnBrowseButtonClicked (NSObject sender)
		{
			OpenDialog(fileNameTextField);
		}

		partial void OnBrowseKeyClicked (NSObject sender)
		{
			OpenDialog(keyTextField);
		}

		private void OpenDialog(NSTextField forField)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.AllowsMultipleSelection = false;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal() == 1)
			{
				var url = dlg.Urls[0];
				if (url != null)
				{
					forField.StringValue = url.Path;
				}
			}
		}
	}
}

