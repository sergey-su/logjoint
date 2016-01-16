
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FormatDetection;

namespace LogJoint.UI
{
	public partial class FormatDetectionPageController : MonoMac.AppKit.NSViewController, IView
	{
		#region Constructors

		// Called when created from unmanaged code
		public FormatDetectionPageController(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public FormatDetectionPageController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public FormatDetectionPageController()
			: base("FormatDetectionPage", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new FormatDetectionPage View
		{
			get
			{
				return (FormatDetectionPage)base.View;
			}
		}

		object IView.PageView
		{
			get { return View; }
		}

		string IView.InputValue
		{
			get { return fileNameTextField.StringValue; }
			set { fileNameTextField.StringValue = value; }
		}

		partial void OnBrowseButtonClicked (NSObject sender)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.AllowsMultipleSelection = false;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal () == 1) 
			{
				var url = dlg.Urls [0];
				if (url != null)
				{
					fileNameTextField.StringValue = url.Path;
				}
			}
		}
	}
}

