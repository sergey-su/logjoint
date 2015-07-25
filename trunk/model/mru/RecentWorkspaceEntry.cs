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

		public RecentWorkspaceEntry(string url, string name, string annotation)
		{
			this.Url = url;
			this.Name = name;
			this.Annotation = annotation;
		}

		string IRecentlyUsedEntity.UserFriendlyName
		{
			get { return "Workspace " + Name; }
		}

		string IRecentlyUsedEntity.Annotation
		{
			get { return Annotation; }
		}
	};
}
