using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Analytics.TimeSeries
{
    public static class MetadataHelper
    {
        public static IEnumerable<TimeSeriesDescriptor> SeriesDescriptorFromMetadata(Type t, TimeSeriesEventAttribute tsAttr)
        {
            var objectAttr = t.GetCustomAttributes(typeof(SourceAttribute), true).OfType<SourceAttribute>().FirstOrDefault();

            var exampleLogLines = (from a in t.GetCustomAttributes(typeof(ExampleLineAttribute), true).OfType<ExampleLineAttribute>()
                                   select a.Value).ToList();

            return from f in t.GetFields()
                          from a in f.GetCustomAttributes(typeof(TimeSeriesAttribute), true).OfType<TimeSeriesAttribute>()
                          select new TimeSeriesDescriptor()
                          {
                              Name = f.Name,
                              ObjectType = tsAttr.Type,
                              Unit = a.Unit,
                              ExampleLogLines = exampleLogLines,
                              Description = a.Description,
                              Scale = a.Scale,
                              Optional = a.Optional,
                              From = a.From,
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

        public static string GetName(this TimeSeries ts)
        {
            return ts.Name;
        }

        public static void ExtractExpressions(Type descriptor, out List<Regex> regexps, out List<string> prefixes, out List<UInt32> numreicIds)
        {
            var exprAttrs = descriptor.GetCustomAttributes(typeof(ExpressionAttribute), true).OfType<ExpressionAttribute>();

            regexps = new List<Regex>();
            prefixes = new List<string>();
            numreicIds = new List<uint>();
            foreach (var exprAttr in exprAttrs)
            {

                if (string.IsNullOrEmpty(exprAttr.Prefix) && exprAttr.NumericId == 0)
                    throw new ArgumentException(string.Format("[Expression] attribute for type {0} doesn't contain a valid prefix/numeric id", descriptor.Name));

                string prefix = exprAttr.Prefix;
                regexps.Add(RegexBuilder.Create(exprAttr.Expression));
                prefixes.Add(prefix);
                numreicIds.Add(exprAttr.NumericId);
            }
        }
    }
}
