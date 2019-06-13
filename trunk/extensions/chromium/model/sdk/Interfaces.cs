using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Chromium
{
	public interface IPluginModel
	{
		void RegisterSource(
			EventsSource<Postprocessing.StateInspector.Event, ChromeDebugLog.Message>.Factory source);
		void RegisterSource(
			TimeSeriesDataSource<ChromeDebugLog.Message>.Factory source);
		void RegisterSource(
			EventsSource<Postprocessing.Timeline.Event, MessagePrefixesPair<ChromeDriver.Message>>.Factory source);
		void RegisterSource(
			EventsSource<Postprocessing.Timeline.Event, ChromeDebugLog.Message>.Factory source);
	};

	public class EventsSource<EventType, MessageType>
	{
		public readonly IEnumerableAsync<EventType[]> Events;
		public readonly List<IMultiplexingEnumerableOpen> MultiplexingEnumerables;

		public EventsSource(
			IEnumerableAsync<EventType[]> events,
			params IMultiplexingEnumerableOpen[] multiplexingEnumerables
		)
		{
			Events = events;
			MultiplexingEnumerables = new List<IMultiplexingEnumerableOpen>(multiplexingEnumerables);
		}

		public delegate EventsSource<EventType, MessageType> Factory(
			IPrefixMatcher matcher,
			IEnumerableAsync<MessageType[]> input,
			ICodepathTracker codepathTracker // todo: structure for args
		);
	};

	public class TimeSeriesDataSource<MessageType>
	{
		public readonly Task Task;
		public readonly List<IMultiplexingEnumerableOpen> MultiplexingEnumerables;

		public TimeSeriesDataSource(
			Task task,
			params IMultiplexingEnumerableOpen[] multiplexingEnumerables
		)
		{
			Task = task;
			MultiplexingEnumerables = new List<IMultiplexingEnumerableOpen>(multiplexingEnumerables);
		}

		public delegate TimeSeriesDataSource<MessageType> Factory(
			IEnumerableAsync<MessageType[]> input,
			Postprocessing.TimeSeries.ICombinedParser parser
		);
	}
}
