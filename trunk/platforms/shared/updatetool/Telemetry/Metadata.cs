using System;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Postprocessing;

namespace LogJoint.UpdateTool.Telemetry
{
	public class FeatureMetadata
	{
		public string Id;

		public Dictionary<int, SubFeatureMetadata> SubFeatures;

		
		public static FeatureMetadata FromId(string id)
		{
			FeatureMetadata ret;
			data.TryGetValue(id, out ret);
			return ret;
		}

		public static IEnumerable<FeatureMetadata> AllFeatures { get { return data.Values; } }

		static FeatureMetadata()
		{
			// todo: add all known templates
			// Add(@"id", typeof(TemplateIdEnum));
		}


		static void Add(string featureId, Type subFeaturesEnumType)
		{
			var feature = new FeatureMetadata() { Id = featureId, SubFeatures = new Dictionary<int, SubFeatureMetadata>() };

			var values = subFeaturesEnumType.GetEnumValues();
			var names = subFeaturesEnumType.GetEnumNames();
			for (int i = 0; i < values.Length; ++i)
			{
				var member = subFeaturesEnumType.GetMember(names[i]).First();
				feature.SubFeatures[Convert.ToInt32(values.GetValue(i))] = new SubFeatureMetadata()
				{
					Feature = feature,
					Id = i,
					Name = names[i],
					IsKnownObsoleteCodepath = member.GetCustomAttributes(typeof(ObsoleteCodepathAttribute), false).FirstOrDefault() != null,
					IsRareCodepath = member.GetCustomAttributes(typeof(RareCodepathAttribute), false).FirstOrDefault() != null,
				};
			}
			feature.SubFeatures.Remove(0); // 0 (None) is reserved to mean "not a feature"

			data.Add(featureId, feature);
		}

		private static Dictionary<string, FeatureMetadata> data = new Dictionary<string, FeatureMetadata>();
	};

	public class SubFeatureMetadata
	{
		public FeatureMetadata Feature;
		public int Id;
		public string Name;
		public bool IsKnownObsoleteCodepath;
		public bool IsRareCodepath;
	};
}
