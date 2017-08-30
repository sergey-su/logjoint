using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UpdateTool.Telemetry
{
	public class Analytics
	{
		public static void AnalizeFeaturesUse(RawDataSet dataSet)
		{
			var subFeaturesUseFrequencies = FeatureMetadata.AllFeatures.SelectMany(f => f.SubFeatures.Values).ToDictionary(
				sf => sf, sf => new UseFrequencyInfo());

			foreach (var dateGroup in 
				dataSet
				.Sessions
				.Select(s => new { Session = s, Date = s.Started.Date })
				.OrderBy(x => x.Date)
				.GroupBy(x => x.Date))
			{
				var totalNrOfLogs = dateGroup.Sum(x => x.Session.TotalNrOfLogs);
				if (totalNrOfLogs == 0)
					continue; // no logs were open this day, ignore this day

				var date = dateGroup.First().Date;

				foreach (var featureGroup in dateGroup.SelectMany(d => d.Session.Features).GroupBy(f => f.Meta))
				{
					var featureMeta = featureGroup.First().Meta;
					
					// Feature with meta featureMeta was used this day (date). 
					// Let's count how often each sub-feature was used this day. 

					var counters = featureMeta.SubFeatures.ToDictionary(sf => sf.Key, sf => 0);
					foreach (var sf in featureGroup.SelectMany(f => f.SubFeatures))
						counters[sf.Meta.Id] += sf.UseCount;

					foreach (var sf in featureMeta.SubFeatures)
					{
						subFeaturesUseFrequencies[sf.Value].AddDataPoint(date,
							(double)counters[sf.Key] / (double)totalNrOfLogs);
					}
				}
			}

			var frequencesXmlDoc = new XDocument(new XElement("root"));
			foreach (var sf in subFeaturesUseFrequencies)
			{
				DateTime firstDate = new DateTime();
				Trendline trend = null;
				var isDeadSubFeature = false;
				if (sf.Value.weeklyAverageUseFrequencies.Count == 0)
					isDeadSubFeature = true;
				else
				{
					firstDate = sf.Value.weeklyAverageUseFrequencies.First().date;
					trend = CalcTend(sf.Value.weeklyAverageUseFrequencies.Select(
						f => new DataPoint() { X = (f.date - firstDate).Days, Y = f.value }).ToArray());
				}
				if (trend != null && Math.Abs(trend.slope) < 1e-6 && Math.Abs(trend.offset) < 1e-6)
				{
					isDeadSubFeature = true;
				}
				else if (trend != null && trend.slope < 0)
				{
					var predictedUnuseDate = firstDate.AddDays(-trend.offset / trend.slope);
					if (predictedUnuseDate < DateTime.Now)
					{
						isDeadSubFeature = true;
					}
				}
				if (isDeadSubFeature)
				{
					var prefix = "";
					if (sf.Key.IsKnownObsoleteCodepath)
						prefix += "Known  ";
					if (sf.Key.IsRareCodepath)
						prefix += "Rare   ";
					if (prefix == "")
						prefix = "       ";
					Console.Write(prefix);
					Console.Write("dead feature: {0}       trend: ", sf.Key.Name);
					if (trend != null)
						Console.WriteLine("{0}*x + {1}", trend.slope, trend.offset);
					else
						Console.WriteLine("N/A");
				}

				foreach (var freq in sf.Value.weeklyAverageUseFrequencies)
				{
					frequencesXmlDoc.Root.Add(new XElement(
						"subFeatureUseFrequency",
						new XElement("featureId", sf.Key.Feature.Id),
						new XElement("subFeatureId", sf.Key.Id),
						new XElement("subFeatureName", sf.Key.Name),
						new XElement("year", freq.date.Year),
						new XElement("month", freq.date.Month),
						new XElement("dayOfYear", freq.date.DayOfYear),
						new XElement("week", freq.date.DayOfYear / 7),
						new XElement("freq", freq.value)
					));
				}
			}
			frequencesXmlDoc.Save(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\lj.telem.sub-features.freqs.xml"));
		}

		static Trendline CalcTend(DataPoint[] data)
		{
			// http://math.stackexchange.com/questions/204020/what-is-the-equation-used-to-calculate-a-linear-trendline

			double xySum = 0, xSum = 0, ySum = 0, xSquareSum = 0;
			foreach (var p in data)
			{
				xySum += p.X * p.Y;
				xSum += p.X;
				ySum += p.Y;
				xSquareSum += p.X * p.X;
			}
			double n = data.Length;

			var slope = (n * xySum - xSum * ySum) / (n * xSquareSum - xSum * xSum);
			var offset = (ySum - slope * xSum) / n;

			return new Trendline() { slope = slope, offset = offset };
		}

		class Trendline
		{
			public double slope, offset;
		};

		class UseFrequencyEntry
		{
			public DateTime date;
			public double value;
		};

		struct DataPoint
		{
			public double X;
			public double Y;
		};

		class UseFrequencyInfo
		{
			public Queue<double> useFrequenciesBuffer = new Queue<double>();
			public List<UseFrequencyEntry> weeklyAverageUseFrequencies = new List<UseFrequencyEntry>();

			public void AddDataPoint(DateTime date, double value)
			{
				useFrequenciesBuffer.Enqueue(value);
				if (useFrequenciesBuffer.Count == 7)
				{
					weeklyAverageUseFrequencies.Add(new UseFrequencyEntry()
					{
						date = date,
						value = useFrequenciesBuffer.Average(),
					});
					useFrequenciesBuffer.Dequeue();
				}
			}
		};
	};
}
