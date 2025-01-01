using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LogJoint.Postprocessing.TimeSeries
{
    internal class EventDescriptor
    {
        public string Name;
        public string ObjectType;
        public string Description;
        public List<string> ExampleLogLines;

        public IEnumerable<EventFieldDescriptor> Fields;

        public void FillEvent(EventBase e)
        {
            e.Name = Name;
            e.ObjectType = ObjectType;
            e.Description = Description;
            e.ExampleLogLines = ExampleLogLines;
        }
    }

    internal class EventFieldDescriptor
    {
        public FieldInfo Field;

        public string Group;

        public TypeConverter Converter;

        public bool FromObjectAddress;
    }

    public class GenericEventParser : ILineParser
    {
        private Type _eventDataType;
        private string _classifierGroup;
        private bool _classifierFromObjectAddress;
        private EventDescriptor _eventDescriptor;

        private readonly Regex _regEx;
        private readonly string _prefix;
        private readonly UInt32 _numericId;


        private GenericEventParser(Type eventDataType, EventAttribute eAttr, Regex regex, string prefix, UInt32 numericId)
        {
            _eventDataType = eventDataType;
            _regEx = regex;
            _prefix = prefix;
            _numericId = numericId;
            _eventDescriptor = MetadataHelper.EventDescriptorFromMetadata(eventDataType, eAttr);

            var objectAttr = _eventDataType.GetCustomAttributes(typeof(SourceAttribute), true).OfType<SourceAttribute>().FirstOrDefault();

            if (objectAttr != null)
            {
                _classifierGroup = objectAttr.From;
                _classifierFromObjectAddress = objectAttr.FromObjectAddress;
            }
        }

        public static GenericEventParser TryCreate(Type eventDataType, Regex regex, string prefix, UInt32 numericId)
        {
            if (prefix == null && numericId == 0)
                return null;

            var eAttr = eventDataType.GetCustomAttributes(typeof(EventAttribute), true).OfType<EventAttribute>().FirstOrDefault();
            if (eAttr == null)
                return null;

            return new GenericEventParser(eventDataType, eAttr, regex, prefix, numericId);
        }

        void ILineParser.Parse(string text, ILineParserVisitor visitor, string objectAddress)
        {
            var match = _regEx.Match(text);
            if (!match.Success)
            {
                return;
            }

            string classifierText = null;
            if (_classifierGroup != null)
            {
                classifierText = match.Groups[_classifierGroup].Value;
            }
            else if (_classifierFromObjectAddress)
            {
                classifierText = objectAddress;
            }

            var e = (EventBase)Activator.CreateInstance(_eventDataType);
            e.ObjectId = classifierText;
            _eventDescriptor.FillEvent(e);

            foreach (var fd in _eventDescriptor.Fields)
            {
                string val;
                if (fd.FromObjectAddress)
                {
                    val = objectAddress;
                }
                else
                {
                    val = match.Groups[fd.Group].Value;
                    if (string.IsNullOrEmpty(val))
                    {
                        // TODO: Check whether field is optional.
                        continue;
                    }
                }
                // TODO: decide whether conversion is necessary...
                fd.Field.SetValue(e, fd.Converter.ConvertFromInvariantString(val));
            }
            visitor.VisitEvent(e);
        }

        string ILineParser.GetPrefix()
        {
            return _prefix;
        }

        Type ILineParser.GetMetadataSource()
        {
            return _eventDataType;
        }

        uint ILineParser.GetNumericId()
        {
            return _numericId;
        }
    }
}
