
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using ObjCRuntime;
using System.Text;
using LogJoint.UI;
using System.Text.RegularExpressions;
using CoreAnimation;
using CoreGraphics;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.MainWindowTabPage
{
	public partial class MainWindowTabPageAdapter : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		#region Constructors

		public MainWindowTabPageAdapter(UI.Presenters.MainForm.IPresenter mainFormPresenter): this()
		{
			mainFormPresenter.AddCustomTab(this.View, "Postprocessing", this);
			mainFormPresenter.TabChanging += (sender, e) =>
			{
				if (e.CustomTabTag == this)
					eventsHandler.OnTabPageSelected();
			};
		}

		// Called when created from unmanaged code
		public MainWindowTabPageAdapter (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowTabPageAdapter (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowTabPageAdapter () : base ("MainWindowTabPage", NSBundle.MainBundle)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		//strongly typed view accessor
		public new MainWindowTabPage View 
		{
			get { return (MainWindowTabPage)base.View; }
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.UpdateControl (ViewControlId id, ControlData data)
		{
			var controls = GetControlsSet (id);
			if (controls != null) 
			{
				UpdateActionLink (controls.content, data.Content, data.Color, !data.Disabled, id);

				if (controls.progress != null)
				{
					controls.progress.Hidden = data.Progress == null;
					controls.progress.DoubleValue = data.Progress.GetValueOrDefault (0d) * 100d;
				}
			}
		}

		void IView.BeginBatchUpdate()
		{
		}

		void IView.EndBatchUpdate()
		{
		}



		void UpdateActionLink(NSLinkLabel lbl, string value, ControlData.StatusColor color, bool enabled, ViewControlId id)
		{
			lbl.SetAttributedContents(value);
			lbl.TextColor = MapColor (color);
			lbl.IsEnabled = enabled;

			if (lbl.LinkClicked == null) 
			{
				lbl.LinkClicked = (s, e) => 
				{
					if (!string.IsNullOrEmpty(e.Link.Tag as string))
					{
						var flags = ClickFlags.None; // todo: flags
						eventsHandler.OnActionClick ((string)e.Link.Tag, id, flags);
					}
				};
			}
		}

		static NSColor MapColor(ControlData.StatusColor c)
		{
			switch (c) 
			{
			case ControlData.StatusColor.Error:
				return NSColor.Red;
			case ControlData.StatusColor.Warning:
				return LogJoint.Drawing.Extensions.ToNSColor(System.Drawing.Color.Salmon);
			case ControlData.StatusColor.Success:
				return LogJoint.Drawing.Extensions.ToNSColor(System.Drawing.Color.FromArgb (0, 176, 80));
			default:
				return NSColor.Text;
			}
		}

		ControlsSet GetControlsSet(ViewControlId id)
		{
			switch (id)
			{
			case ViewControlId.StateInspector:
				return new ControlsSet (stateInspectorAction1, stateInspectorProgressIndicator);
			case ViewControlId.Timeline:
				return new ControlsSet (timelineAction1, timelineProgressIndicator);
			case ViewControlId.Sequence:
				return new ControlsSet (sequenceAction1, sequenceProgressIndicator);
			case ViewControlId.TimeSeries:
				return new ControlsSet (timeSeriesAction1, timeSeriesProgressIndicator);
			case ViewControlId.Correlate:
				return new ControlsSet (correlationAction1, correlationProgressIndicator);
			case ViewControlId.LogsCollectionControl1:
				return new ControlsSet (cloudDownloaderAction1, cloudLogsDownloaderProgressIndicator);
			case ViewControlId.LogsCollectionControl2:
				return new ControlsSet (cloudDownloaderAction2, cloudDownloaderProgressIndicator2);
			case ViewControlId.AllPostprocessors:
				return new ControlsSet (allPostprocessorsAction, allPostprocessorsProgressIndicator);
			case ViewControlId.LogsCollectionControl3:
				return new ControlsSet (openGenericLogAction, null);
			}
			return null;
		}

		class ControlsSet
		{
			public NSLinkLabel content;
			public NSProgressIndicator progress;
			public ControlsSet(NSLinkLabel content, NSProgressIndicator progress)
			{
				this.content = content;
				this.progress = progress;
			}
		};
	}
}

