namespace LogJoint.Postprocessing.Timeline
{
	public static class TimelineExtensions
	{
		public static IEnumerableAsync<Event[]> TrackTemplates(this ICodepathTracker codepathTracker, IEnumerableAsync<Event[]> events)
		{
			return events.Select(batch =>
			{
				if (codepathTracker != null)
					foreach (var e in batch)
						codepathTracker.RegisterUsage(e.TemplateId);
				return batch;
			});
		}
	}
}
