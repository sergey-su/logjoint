using LogJoint.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Diagnostics;

namespace LogJoint.UI
{
	class ModelHost: IModelHost
	{
		public ModelHost(LJTraceSource tracer, MainWindowAdapter mainWindow)
		{
			this.tracer = tracer;
		}

		public void Init(Presenters.LogViewer.IPresenter viewerPresenter, IViewUpdates viewUpdates)
		{
			this.viewerPresenter = viewerPresenter;
			this.viewUpdates = viewUpdates;
		}

		void IModelHost.OnIdleWhileShifting()
		{
			tracer.Info("running loop: start");
			NSRunLoop.Main.RunUntil(NSDate.Now.AddSeconds(0.1));
			tracer.Info("running loop: end");
		}

		void IModelHost.OnUpdateView()
		{
			if (viewUpdates != null)
				viewUpdates.RequestUpdate();
		}

		void IModelHost.SetCurrentViewTime(DateTime? time, NavigateFlag flags, ILogSource preferredSource)
		{
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
