
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using LogJoint.Drawing;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.MainWindowTabPage
{
	public partial class MainWindowTabPageAdapter : AppKit.NSViewController, IView
	{
		IViewModel viewModel;

		public MainWindowTabPageAdapter () : base ("MainWindowTabPage", NSBundle.MainBundle)
		{
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;
			var updateControls = Updaters.Create (
				() => viewModel.ControlsState,
				state => {
					foreach (var s in state)
						UpdateControl (s.Key, s.Value);
				}
			);
			viewModel.ChangeNotification.CreateSubscription (updateControls);
		}

		void UpdateControl (ViewControlId id, ControlData data)
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
						viewModel.OnActionClick ((string)e.Link.Tag, id, flags);
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
				return Color.Salmon.ToNSColor();
			case ControlData.StatusColor.Success:
				return Color.FromArgb (0, 176, 80).ToNSColor();
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

