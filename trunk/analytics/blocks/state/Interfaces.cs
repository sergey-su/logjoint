
using System;
using System.Collections.Generic;

namespace LogJoint.Analytics.StateInspector
{
	public abstract class Event: ITagged
	{
		public object Trigger;
		public readonly string ObjectId;
		public HashSet<string> Tags { get { return tags; } set { tags = value; } }
		public readonly ObjectTypeInfo ObjectType;
		public readonly int TemplateId;

		public Event(object trigger, string objectId, ObjectTypeInfo objectType, int templateId)
		{
			ValidateObjectId(objectId);
			Trigger = trigger;
			ObjectId = objectId;
			ObjectType = objectType;
			TemplateId = templateId;
		}

		public abstract void Visit(IEventsVisitor visitor);

		public override string ToString()
		{
			var stringifier = new EventsStringifier();
			Visit(stringifier);
			return stringifier.Output.ToString();
		}

		void ValidateObjectId(string objectId)
		{
			if (string.IsNullOrEmpty(objectId))
				throw new ArgumentException("objectId");
		}

		HashSet<string> tags;
	};

	public class ObjectCreation : Event
	{
		/// <summary>
		/// Weakness determines what happens when tree builder
		/// processes object creation event for an object that is already created.
		/// Weak creation event leaves existing object.
		/// Strong creation event finalizes existing object and creates new one.
		/// Default: false (strong)
		/// </summary>
		public readonly bool IsWeak;
		public readonly string DisplayName;

		public ObjectCreation(object trigger, string objectId, ObjectTypeInfo objectTypeInfo, 
				int templateId = 0, bool isWeak = false, string displayName = null) :
			base(trigger, objectId, objectTypeInfo, templateId)
		{
			IsWeak = isWeak;
			DisplayName = displayName;
		}

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public class ObjectDeletion : Event
	{
		public ObjectDeletion(object trigger, string objectId, ObjectTypeInfo objectTypeInfo, int templateId = 0)
			: base(trigger, objectId, objectTypeInfo, templateId)
		{
		}

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public enum ValueType
	{
		Scalar,
		Reference,
		ThreadReference,
		UserHash
	};

	public class PropertyChange : Event
	{
		public readonly string PropertyName;
		public readonly string Value;
		public readonly ValueType ValueType;
		public readonly string OldValue;

		public PropertyChange(object trigger, string objectId, ObjectTypeInfo objectTypeInfo, string propertyName = null, string value = null, ValueType valueType = ValueType.Scalar, string oldValue = null, int templateId = 0)
			: base(trigger, objectId, objectTypeInfo, templateId)
		{
			PropertyName = propertyName;
			Value = value;
			OldValue = oldValue;
			ValueType = valueType;
		}

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public class ParentChildRelationChange : Event
	{
		public readonly string NewParentObjectId;
		public readonly bool IsWeak;

		public ParentChildRelationChange(object trigger, string objectId, ObjectTypeInfo objectTypeInfo, string newParentObjectId = null, int templateId = 0, bool isWeak = false)
			: base(trigger, objectId, objectTypeInfo, templateId)
		{
			NewParentObjectId = newParentObjectId;
			IsWeak = isWeak;
		}

		public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
	};

	public class ObjectTypeInfo
	{
		public readonly string CommentPropertyName;
		public readonly string PrimaryPropertyName;
		public readonly string TypeName;
		public readonly bool IsTimeless;
		[Obsolete("Use CommentPropertyName instead")]
		public string DisplayIdPropertyName { get { return CommentPropertyName; } }


		public ObjectTypeInfo(string type, string displayIdPropertyName = null, string primaryPropertyName = null, bool isTimeless = false)
		{
			TypeName = type;
			CommentPropertyName = displayIdPropertyName;
			PrimaryPropertyName = primaryPropertyName;
			IsTimeless = isTimeless;
		}

		internal static bool Equals(ObjectTypeInfo obj1, ObjectTypeInfo obj2)
		{
			return 
				obj1.TypeName == obj2.TypeName
			 && obj1.IsTimeless == obj2.IsTimeless
			 && obj1.PrimaryPropertyName == obj2.PrimaryPropertyName
			 && obj1.CommentPropertyName == obj2.CommentPropertyName;
		}
	};

	public interface IEventsVisitor
	{
		void Visit(ObjectCreation objectCreation);
		void Visit(ObjectDeletion objectDeletion);
		void Visit(PropertyChange propertyChange);
		void Visit(ParentChildRelationChange parentChildRelationChange);
	};
}
