using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using System.Linq;
using System.Collections.Generic;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;

namespace LogJoint.UI.Presenters.Tests
{
	[TestFixture]
	public class TimelineVisualizerPresenterTests
	{
		IPresenter presenter;
		IViewEvents eventsHandler;
		ITimelineVisualizerModel model;
		IView view;
		Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorVisualizer;
		Postprocessing.Common.IPresentationObjectsFactory presentationObjectsFactory;
		LoadedMessages.IPresenter loadedMessagesPresenter;
		IBookmarks bookmarks;
		Persistence.IStorageManager storageManager;
		IPresentersFacade presentersFacade;
		IUserNamesProvider userNamesProvider;

		[SetUp] 
		public void Init()
		{
			view = Substitute.For<IView>();
			model = Substitute.For<ITimelineVisualizerModel>();
			view.When(v => v.SetEventsHandler(Arg.Any<IViewEvents>())).Do(x => eventsHandler = x.Arg<IViewEvents>());
			presenter = new TimelineVisualizerPresenter(
				model,
				view,
				stateInspectorVisualizer,
				presentationObjectsFactory,
				loadedMessagesPresenter,
				bookmarks,
				storageManager,
				presentersFacade,
				userNamesProvider
			);
		}
	}
}
