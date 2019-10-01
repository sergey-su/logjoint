
using LogJoint.Postprocessing;
using System.Collections.Generic;

namespace LogJoint.Chromium
{
	public class PluginModel : IPluginModel
	{
		public readonly List<EventsSource<Postprocessing.StateInspector.Event, ChromeDebugLog.Message>.Factory> ChromeDebugStateEventSources =
			new List<EventsSource<Postprocessing.StateInspector.Event, ChromeDebugLog.Message>.Factory>();
		public readonly List<TimeSeriesDataSource<ChromeDebugLog.Message>.Factory> ChromeDebugTimeSeriesSources =
			new List<TimeSeriesDataSource<ChromeDebugLog.Message>.Factory>();
		public readonly List<EventsSource<Postprocessing.Timeline.Event, MessagePrefixesPair<ChromeDriver.Message>>.Factory> ChromeDriverTimeLineEventSources =
			new List<EventsSource<Postprocessing.Timeline.Event, MessagePrefixesPair<ChromeDriver.Message>>.Factory>();
		public readonly List<EventsSource<Postprocessing.Timeline.Event, ChromeDebugLog.Message>.Factory> ChromeDebugLogTimeLineEventSources =
			new List<EventsSource<Postprocessing.Timeline.Event, ChromeDebugLog.Message>.Factory>();
		public readonly List<EventsSource<Postprocessing.Messaging.Event, ChromeDebugLog.Message>.Factory> ChromeDebugLogMessagingEventSources =
			new List<EventsSource<Postprocessing.Messaging.Event, ChromeDebugLog.Message>.Factory>();

		void IPluginModel.RegisterSource(EventsSource<Postprocessing.StateInspector.Event, ChromeDebugLog.Message>.Factory source)
		{
			ChromeDebugStateEventSources.Add(source);
		}

		void IPluginModel.RegisterSource(TimeSeriesDataSource<ChromeDebugLog.Message>.Factory source)
		{
			ChromeDebugTimeSeriesSources.Add(source);
		}
		void IPluginModel.RegisterSource(
			EventsSource<Postprocessing.Timeline.Event, MessagePrefixesPair<ChromeDriver.Message>>.Factory source)
		{
			ChromeDriverTimeLineEventSources.Add(source);
		}
		void IPluginModel.RegisterSource(
			EventsSource<Postprocessing.Timeline.Event, ChromeDebugLog.Message>.Factory source)
		{
			ChromeDebugLogTimeLineEventSources.Add(source);
		}
		void IPluginModel.RegisterSource(
			EventsSource<Postprocessing.Messaging.Event, ChromeDebugLog.Message>.Factory source)
		{
			ChromeDebugLogMessagingEventSources.Add(source);
		}
	};
}
