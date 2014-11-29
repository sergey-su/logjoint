using LogJoint.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class ModelHost: IModelHost
	{
		public ModelHost(LJTraceSource tracer)
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
			Application.DoEvents();
		}

		void IModelHost.OnUpdateView()
		{
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
