
namespace LogJoint.MRU
{
	public static class Extensions
	{
		public static void RegisterRecentLogEntry(this MRU.IRecentlyUsedEntities mruLogsList, ILogSource src)
		{
			mruLogsList.RegisterRecentLogEntry(src.Provider, src.Annotation);
		}
	}
}
