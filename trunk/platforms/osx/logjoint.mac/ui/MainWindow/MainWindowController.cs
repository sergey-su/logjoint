
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Constructors

		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowController () : base ("MainWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		public void Init(IModel model)
		{
			this.model = model;
		}

		#endregion

		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			logViewerControlAdapter = new LogViewerControlAdapter ();
			PutCustomControlToPlaceholder(logViewerControlAdapter.View, logViewerPlaceholder);
		}

		static void PutCustomControlToPlaceholder(NSView customControlView, NSView placeholder)
		{
			placeholder.AddSubview (customControlView);
			//customControlView.Bounds = placeholder.Bounds;
			customControlView.AutoresizingMask = 
				NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		}

		partial void onButtonClicked(NSObject sender)
		{
			if (ls != null)
			{
				Window.Title = ls.Provider.Stats.State.ToString();
				return;
			}
			var f = model.LogProviderFactoryRegistry.Find(
				"Microsoft", "TextWriterTraceListener");
			ls = model.CreateLogSource(f, ((IFileBasedLogProviderFactory)f).CreateParams("/Users/sergeysu/lj-debug.log"));
		}

		IModel model;
		ILogSource ls;
		LogViewerControlAdapter logViewerControlAdapter;
	}
}

