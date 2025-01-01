using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Correlation
{
    public class NodeSolution : INodeSolution
    {
        public TimeSpan BaseDelta { get; private set; }
        public IReadOnlyList<TimeDeltaEntry> TimeDeltas { get; private set; }
        public int NrOnConstraints { get; private set; }
        public static string XmlName { get { return xmlName; } }

        internal static string xmlName = "solution";

        internal NodeSolution(TimeSpan baseDelta, IReadOnlyList<TimeDeltaEntry> timeDeltas, int nrOnConstraints)
        {
            BaseDelta = baseDelta;
            TimeDeltas = timeDeltas;
            NrOnConstraints = nrOnConstraints;
        }

        public XElement Serialize()
        {
            return new XElement(
                xmlName,
                new XAttribute("base-delta", BaseDelta.Ticks),
                new XAttribute("nr-of-constraints", NrOnConstraints),
                (TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>()).Select(d =>
                    new XElement("delta",
                        new XAttribute("at", d.At.Ticks),
                        new XAttribute("value", d.Delta.Ticks)
                    )
                )
            );
        }

        public NodeSolution(XElement node)
        {
            BaseDelta = TimeSpan.FromTicks(long.Parse(node.Attribute("base-delta").Value));
            NrOnConstraints = int.Parse(node.Attribute("nr-of-constraints").Value);
            TimeDeltas = node.Elements("delta").Select(de => new TimeDeltaEntry(
               new DateTime(long.Parse(de.Attribute("at").Value), DateTimeKind.Unspecified),
               TimeSpan.FromTicks(long.Parse(de.Attribute("value").Value)),
               null,
               null
           )).ToList();
        }

        public bool Equals(INodeSolution other)
        {
            return
                BaseDelta == other.BaseDelta
                && Enumerable.SequenceEqual(
                    TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>(),
                    other.TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>(),
                    TimeDeltaEntryComparer.Instance
                );
        }

        class TimeDeltaEntryComparer : IEqualityComparer<TimeDeltaEntry>
        {
            public static TimeDeltaEntryComparer Instance = new TimeDeltaEntryComparer();

            bool IEqualityComparer<TimeDeltaEntry>.Equals(TimeDeltaEntry x, TimeDeltaEntry y)
            {
                return x.At == y.At && x.Delta == y.Delta;
            }

            int IEqualityComparer<TimeDeltaEntry>.GetHashCode(TimeDeltaEntry obj)
            {
                return obj.At.GetHashCode() ^ obj.Delta.GetHashCode();
            }
        };
    };
}