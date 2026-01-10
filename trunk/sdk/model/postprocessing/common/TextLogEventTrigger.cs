using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LogJoint.Postprocessing
{
    public class TextLogEventTrigger : ITriggerStreamPosition, ITriggerTime
    {
        public readonly long StreamPosition;
        public readonly MessageTimestamp Timestamp;

        public TextLogEventTrigger(XElement evtElement) : this(
            long.Parse(evtElement.Attribute(triggerPositionAttr)?.Value ?? ""),
            MessageTimestamp.ParseFromLoselessFormat(evtElement.Attribute(triggerTimestampAttr)?.Value ?? ""))
        {
        }

        public TextLogEventTrigger(long position, MessageTimestamp timestamp)
        {
            this.StreamPosition = position;
            this.Timestamp = new MessageTimestamp(timestamp.ToUnspecifiedTime());
        }

        public static TextLogEventTrigger Make<T>(T trigger) where T : ITriggerStreamPosition, ITriggerTime
        {
            return new TextLogEventTrigger(trigger.StreamPosition, new MessageTimestamp(trigger.Timestamp));
        }

        public static TextLogEventTrigger FromUnknownTrigger(object obj)
        {
            var pos = obj as ITriggerStreamPosition;
            var time = obj as ITriggerTime;
            if (pos == null || time == null)
                throw new ArgumentException();
            return new TextLogEventTrigger(pos.StreamPosition, new MessageTimestamp(time.Timestamp));
        }

#if !SERVICE
        public TextLogEventTrigger(IBookmark bmk)
        {
            StreamPosition = bmk.Position;
            var timeOffset = bmk.Thread?.LogSource?.TimeOffsets?.Inverse();
            Timestamp = new MessageTimestamp((timeOffset != null ? bmk.Time.Adjust(timeOffset) : bmk.Time).ToUnspecifiedTime());
        }
#endif

        internal TextLogEventTrigger(XmlReader reader)
            : this(
                long.Parse(reader.GetAttribute(triggerPositionAttrShort) ?? ""),
                MessageTimestamp.ParseFromLoselessFormat(reader.GetAttribute(triggerTimestampAttrShort) ?? ""))
        {
        }

        long ITriggerStreamPosition.StreamPosition
        {
            get { return StreamPosition; }
        }

        DateTime ITriggerTime.Timestamp
        {
            get { return Timestamp.ToUnspecifiedTime(); }
        }

        public int CompareTo(TextLogEventTrigger t)
        {
            int x;
            if ((x = Math.Sign(this.StreamPosition - t.StreamPosition)) != 0)
                return x;
            if ((x = MessageTimestamp.Compare(this.Timestamp, t.Timestamp)) != 0)
                return x;
            return 0;
        }

        public static Func<XElement, object> DeserializerFunction { get { return deserializerFunction; } }

        public void Save(XElement elt)
        {
            elt.SetAttributeValue(triggerPositionAttr, StreamPosition);
            elt.SetAttributeValue(triggerTimestampAttr, Timestamp.StoreToLoselessFormat());
        }

        internal void Save(XmlWriter w)
        {
            w.WriteAttributeString(triggerPositionAttrShort, StreamPosition.ToString());
            w.WriteAttributeString(triggerTimestampAttrShort, Timestamp.StoreToLoselessFormat());
        }

        const string triggerPositionAttrShort = "position";
        const string triggerTimestampAttrShort = "timestamp";
        const string triggerPositionAttr = "trigger.position";
        const string triggerTimestampAttr = "trigger.timestamp";
        static readonly Func<XElement, object> deserializerFunction = evtElement => new TextLogEventTrigger(evtElement);
    };

    [XmlRoot("trigger")]
    public class XmlSerializableTextLogEventTrigger : IXmlSerializable
    {
        TextLogEventTrigger? data;

        public XmlSerializableTextLogEventTrigger(TextLogEventTrigger data)
        {
            this.data = data;
        }

        public XmlSerializableTextLogEventTrigger()
        {
        }

        public TextLogEventTrigger? Data { get { return data; } }

        System.Xml.Schema.XmlSchema? IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            data = new TextLogEventTrigger(reader);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            data?.Save(writer);
        }
    };
}
