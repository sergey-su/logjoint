using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogJoint.UpdateTool.Telemetry
{
	namespace Schema
	{
		public class Session
		{
			public string Id;
			public DateTime Started;
			public int TotalNrOfLogs;
			// todo: read more fields from azure table

			public List<Feature> Features = new List<Feature>();
			public List<Exception> Exceptions = new List<Exception>();
		};

		public class Feature
		{
			public FeatureMetadata Meta;
			public Session Session;

			public int UseCount;

			public List<SubFeature> SubFeatures = new List<SubFeature>();
		};

		public class SubFeature
		{
			public SubFeatureMetadata Meta;
			public Feature Feature;

			public int UseCount;
		};

		// todo: read exceptions
		public class Exception
		{
			public Session Session;
			public string FullText;
		};
	};

	public class RawDataSet
	{
		public List<Schema.Session> Sessions = new List<Schema.Session>();
		public List<Schema.Feature> Features = new List<Schema.Feature>();
		public List<Schema.SubFeature> SubFeatures = new List<Schema.SubFeature>();
		public List<string> Errors = new List<string>(); // todo: print errors

		public void HandleTelemetryEntry(AzureStorageEntry entry)
		{
			var session = new Schema.Session()
			{
				Id = entry.id,
				Started = entry.SessionStartDate,
				TotalNrOfLogs = entry.TotalNrOfLogs,
			};
			Sessions.Add(session);
			HandleUsedFeatures (session, entry.usedFeatures);
			HandleExceptions (session, entry.exceptions);
		}

		public void HandleUsedFeatures(Schema.Session session, string usedFeatures)
		{
			foreach (var featureStr in (usedFeatures ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var featureMatch = featureRegex.Match(featureStr);
				if (!featureMatch.Success)
				{
					Errors.Add("bad feature string: " + featureStr);
					continue;
				}
				var featureId = featureMatch.Groups[1].Value;
				var feature = new Schema.Feature()
				{
					Session = session,
					Meta = FeatureMetadata.FromId(featureId)
				};
				if (feature.Meta == null)
				{
					Errors.Add("unknown feature: " + featureId);
					continue;
				}
				session.Features.Add(feature);
				foreach (var subFeatureStr in featureMatch.Groups[3].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					var subFeatureMatch = subFeatureRegex.Match(subFeatureStr);
					if (!subFeatureMatch.Success)
					{
						Errors.Add("bad subfeature string: " + subFeatureStr);
						continue;
					}
					SubFeatureMetadata sfm;
					if (!feature.Meta.SubFeatures.TryGetValue(int.Parse(subFeatureMatch.Groups[1].Value), out sfm))
					{
						Errors.Add("unkonwn subfeature string: " + subFeatureMatch.Groups[1].Value);
						continue;
					}
					var subFeature = new Schema.SubFeature()
					{
						Feature = feature,
						Meta = sfm,
						UseCount = int.Parse(subFeatureMatch.Groups[2].Value)
					};
					SubFeatures.Add(subFeature);
					feature.SubFeatures.Add(subFeature);
				}
			}
		}

		public void HandleExceptions(Schema.Session session, string exceptions)
		{
			if (string.IsNullOrEmpty (exceptions))
				return;
			exceptions.ToLower ();
		}

		readonly Regex featureRegex = new Regex(@"^(?<name>[^\:]+)\:(?<useCount>\d+)(\ \{(?<subFeatures>[^\}]+)\})?",
			RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		readonly Regex subFeatureRegex = new Regex(@"^(?<id>\d+)\:(?<useCount>\d+)",
			RegexOptions.ExplicitCapture | RegexOptions.Compiled);
	};
}
