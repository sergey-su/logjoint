using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Postprocessing.TimeSeries
{
    public static class MetadataHelper
    {
        public static IEnumerable<TimeSeriesDescriptor> SeriesDescriptorFromMetadata(Type t, TimeSeriesEventAttribute tsAttr)
        {
            var objectAttr = t.GetCustomAttributes(typeof(SourceAttribute), true).OfType<SourceAttribute>().FirstOrDefault();

            var exampleLogLines = (from a in t.GetCustomAttributes(typeof(ExampleLineAttribute), true).OfType<ExampleLineAttribute>()
                                   select a.Value).ToList();

			string getUnitGroup(string value)
			{
				var m = Regex.Match(value ?? "", @"\<(\w+)\>");
				return m.Success ? m.Groups[1].Value : null;
			};

            return from f in t.GetFields()
                          from a in f.GetCustomAttributes(typeof(TimeSeriesAttribute), true).OfType<TimeSeriesAttribute>()
						  let unitGroup = getUnitGroup(a.Unit)
                          select new TimeSeriesDescriptor()
                          {
                              Name = a.Name == null ? f.Name : null,
                              NameFromGroup = a.Name,
                              ObjectType = tsAttr.Type,
                              Unit = unitGroup == null ? a.Unit : null,
							  UnitFromGroup = unitGroup,
                              ExampleLogLines = exampleLogLines,
                              Description = a.Description,
                              Scale = a.Scale,
                              Optional = a.Optional,
                              From = a.From ?? f.Name,
                              ObjectIdFromGroup = (objectAttr != null) ? objectAttr.From : null,
                              ObjectIdFromAddress = (objectAttr != null) ? objectAttr.FromObjectAddress : false,
                          };
        }

        internal static EventDescriptor EventDescriptorFromMetadata(Type t, EventAttribute eAttr)
        {
            var result = new EventDescriptor()
            {
                Name = eAttr.Name,
                ObjectType = eAttr.Type,
                Description = eAttr.Description,
                ExampleLogLines = (from a in t.GetCustomAttributes(typeof(ExampleLineAttribute), true).OfType<ExampleLineAttribute>()
                                   select a.Value).ToList(),
            };

            result.Fields = from f in t.GetFields()
                      from a in f.GetCustomAttributes(typeof(EventFieldAttribute), true).OfType<EventFieldAttribute>()
                      select new EventFieldDescriptor()
                      {
                          Field = f,
                          Group = a.From,
                          Converter = TypeDescriptor.GetConverter(f.FieldType),
                          FromObjectAddress = a.FromObjectAddress
                      };
            return result;
        }

        public static string GetName(this TimeSeriesData ts)
        {
            return ts.Name;
        }
    }
}
