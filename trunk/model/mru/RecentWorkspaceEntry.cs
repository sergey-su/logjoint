using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.MRU
{
	public class RecentWorkspaceEntry : IRecentlyUsedEntity
	{
		public readonly string Url;
		public readonly string Name;
		public readonly string Annotation;
		public readonly DateTime? UseTimestampUtc;

		public RecentWorkspaceEntry(string url, string name, string annotation, DateTime? useTimestampUtc)
		{
			this.Url = url;
			this.Name = name;
			this.Annotation = annotation;
			this.UseTimestampUtc = useTimestampUtc;
		}

		string IRecentlyUsedEntity.UserFriendlyName
		{
			get { return "Workspace " + Name; }
		}

		string IRecentlyUsedEntity.Annotation
		{
			get { return Annotation; }
		}

		RecentlyUsedEntityType IRecentlyUsedEntity.Type
		{
			get { return RecentlyUsedEntityType.Workspace; }
		}

		DateTime? IRecentlyUsedEntity.UseTimestampUtc
		{
			get { return UseTimestampUtc; }
		}

		IConnectionParams IRecentlyUsedEntity.ConnectionParams
		{
			get { return null; }
		}
	};
}
