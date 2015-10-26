using LogJoint.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace LogJoint.UI
{
	class ModelHost: IModelHost
	{
		MainWindowAdapter mainWindow;

		public ModelHost(LJTraceSource tracer, MainWindowAdapter mainWindow)
		{
			this.tracer = tracer;
			this.mainWindow = mainWindow;
		}

		public void Init(Presenters.LogViewer.IPresenter viewerPresenter, IViewUpdates viewUpdates)
		{
			this.viewerPresenter = viewerPresenter;
			this.viewUpdates = viewUpdates;
		}

		void IModelHost.OnIdleWhileShifting()
		{
			mainWindow.Window.SetTimer(TimeSpan.FromMilliseconds(30), () => {
				tracer.Info("OnIdleWhileShifting: aborting modal");
				NSApplication.SharedApplication.StopModal();
			});
			tracer.Info("OnIdleWhileShifting: starting modal");
			NSApplication.SharedApplication.RunModalForWindow(mainWindow.Window);
			tracer.Info("OnIdleWhileShifting: modal ended");
		}

		void IModelHost.OnUpdateView()
		{
			if (viewUpdates != null)
				viewUpdates.RequestUpdate();
		}

		void IModelHost.SetCurrentViewTime(DateTime? time, NavigateFlag flags, ILogSource preferredSource)
		{
			return;

			using (tracer.NewFrame)
			{
				tracer.Info("time={0}, flags={1}", time, flags);
				NavigateFlag origin = flags & NavigateFlag.OriginMask;
				NavigateFlag align = flags & NavigateFlag.AlignMask;
				switch (origin)
				{
					case NavigateFlag.OriginDate:
						tracer.Info("Selecting the line at the certain time");
						viewerPresenter.SelectMessageAt(time.Value, align, preferredSource);
						break;
					case NavigateFlag.OriginStreamBoundaries:
						switch (align)
						{
							case NavigateFlag.AlignTop:
								tracer.Info("Selecting the first line");
								viewerPresenter.SelectFirstMessage();
								break;
							case NavigateFlag.AlignBottom:
								tracer.Info("Selecting the last line");
								viewerPresenter.SelectLastMessage();
								break;
						}
						break;
				}
			}
		}

		IViewUpdates viewUpdates;
		LJTraceSource tracer;
		Presenters.LogViewer.IPresenter viewerPresenter;
	}
}
