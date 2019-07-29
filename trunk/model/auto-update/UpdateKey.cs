using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.AutoUpdate
{
	class UpdateKey : IUpdateKey
	{
		readonly string appEtag;
		readonly IReadOnlyDictionary<string, string> pluginEtags;

		public UpdateKey(string appEtag, IReadOnlyDictionary<string, string> pluginEtags)
		{
			this.appEtag = appEtag;
			this.pluginEtags = pluginEtags ?? throw new NullReferenceException(nameof(pluginEtags));
		}

		public static readonly UpdateKey Null = new UpdateKey(null, new Dictionary<string, string>());

		bool IUpdateKey.Equals(IUpdateKey other)
		{
			if (!(other is UpdateKey otherKey))
				return false;
			if (this == Null && other == Null)
				return true;
			if (this == Null || other == Null)
				return false;
			if (appEtag != otherKey.appEtag)
				return false;
			if (pluginEtags.Count != otherKey.pluginEtags.Count)
				return false;
			foreach (var p in pluginEtags)
				if (!otherKey.pluginEtags.TryGetValue(p.Key, out var pluginEtag) || pluginEtag != p.Value)
					return false;
			return true;
		}

		public override string ToString()
		{
			if (this == Null)
				return "(Null)";
			return $"app={appEtag},plugins={string.Join(",", pluginEtags.Select(x => (x.Key, x.Value)))}";
		}
	};
}
