using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Postprocessing.StateInspector.SerializationCommon;

namespace LogJoint.Postprocessing.StateInspector
{
	public class EventsSerializer: IEventsVisitor, IEventsSerializer
	{
		public EventsSerializer(Action<object, XElement> triggerSerializer = null)
		{
			this.triggerSerializer = triggerSerializer;
		}

		public ICollection<XElement> Output { get { return output; } }

		void IEventsVisitor.Visit(ObjectCreation objectCreation)
		{
			CreateElement(SC.Elt_ObjectCreation, objectCreation.Trigger, 
				new XAttribute(SC.Attr_ObjectId, objectCreation.ObjectId),
				MakeNullableAttr(SC.Attr_ObjectType, objectCreation.ObjectType.TypeName),
				MakeNullableAttr(SC.Attr_CommentPropertyName, objectCreation.ObjectType.CommentPropertyName),
				MakeNullableAttr(SC.Attr_PrimaryPropertyName, objectCreation.ObjectType.PrimaryPropertyName),
				objectCreation.ObjectType.IsTimeless ? new XAttribute(SC.Attr_IsTimeless, "1") : null,
				objectCreation.IsWeak ? new XAttribute(SC.Attr_IsWeak, "1") : null,
				MakeTagsAttr(objectCreation),
				MakeNullableAttr(SC.Attr_DisplayNamePropertyName, objectCreation.DisplayName)
			);
		}

		void IEventsVisitor.Visit(ObjectDeletion objectDeletion)
		{
			CreateElement(SC.Elt_ObjectDeletion, objectDeletion.Trigger,
				new XAttribute(SC.Attr_ObjectId, objectDeletion.ObjectId),
				MakeTagsAttr(objectDeletion)
			);
		}

		void IEventsVisitor.Visit(PropertyChange propertyChange)
		{
			CreateElement(SC.Elt_PropertyChange, propertyChange.Trigger,
				new XAttribute(SC.Attr_ObjectId, propertyChange.ObjectId),
				new XAttribute(SC.Attr_PropertyName, propertyChange.PropertyName),
				MakeNullableAttr(SC.Attr_Value, propertyChange.Value),
				MakeNullableAttr(SC.Attr_OldValue, propertyChange.OldValue),
				new XAttribute(SC.Attr_ValueType, propertyChange.ValueType.ToString().ToLower()),
				MakeTagsAttr(propertyChange)
			);
		}

		void IEventsVisitor.Visit(ParentChildRelationChange parentChildRelationChange)
		{
			CreateElement(SC.Elt_ParentChildRelationChange, parentChildRelationChange.Trigger,
				new XAttribute(SC.Attr_ObjectId, parentChildRelationChange.ObjectId),
				MakeNullableAttr(SC.Attr_NewParentObjectId, parentChildRelationChange.NewParentObjectId),
				parentChildRelationChange.IsWeak ? new XAttribute(SC.Attr_IsWeak, "1") : null,
				MakeTagsAttr(parentChildRelationChange)
			);
		}

		static XAttribute MakeNullableAttr(string attrName, object value)
		{
			if (value == null)
				return null;
			return new XAttribute(attrName, value);
		}

		XElement CreateElement(string name, object trigger, params XAttribute[] attrs)
		{
			var element = new XElement(name, attrs.Where(a => a != null).ToArray());
			if (trigger != null && triggerSerializer != null)
				triggerSerializer(trigger, element);
			output.Add(element);
			return element;
		}

		static XAttribute MakeTagsAttr(Event e)
		{
			return new XAttribute(SC.Attr_Tags, string.Join(" ", e.Tags ?? noTags));
		}


		readonly List<XElement> output = new List<XElement>();
		readonly Action<object, XElement> triggerSerializer;
		readonly static HashSet<string> noTags = new HashSet<string>();
	}
}
