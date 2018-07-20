using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Analytics.StateInspector.SerializationCommon;

namespace LogJoint.Analytics.StateInspector
{
	public class EventsDeserializer
	{
		public EventsDeserializer(Func<XElement, object> triggerDeserializer = null)
		{
			this.triggerDeserializer = triggerDeserializer;
		}

		public IEnumerable<Event> Deserialize(XElement root)
		{
			foreach (var elt in root.Elements())
			{
				Event ret = null;
				switch (elt.Name.LocalName)
				{
					case SC.Elt_ObjectCreation:
						ret = new ObjectCreation(
							MakeTrigger(elt),
							objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId)),
							objectInfoPool.Intern(new ObjectTypeInfo(
								Attr(elt, SC.Attr_ObjectType),
								Attr(elt, SC.Attr_CommentPropertyName),
								Attr(elt, SC.Attr_PrimaryPropertyName),
								(Attr(elt, SC.Attr_IsTimeless) ?? "0") == "1"
							)),
							isWeak: (Attr(elt, SC.Attr_IsWeak) ?? "0") == "1",
							displayName: Attr(elt, SC.Attr_DisplayNamePropertyName)
						);
						break;
					case SC.Elt_ObjectDeletion:
						ret = new ObjectDeletion(MakeTrigger(elt), objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId)), null);
						break;
					case SC.Elt_PropertyChange:
						ret = new PropertyChange(MakeTrigger(elt),
							objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId)), 
							null,
							propertyName: propNamesPool.Intern(Attr(elt, SC.Attr_PropertyName)), 
							value: Attr(elt, SC.Attr_Value),
							oldValue: Attr(elt, SC.Attr_OldValue),
							valueType: ToValueType(Attr(elt, SC.Attr_ValueType)));
						break;
					case SC.Elt_ParentChildRelationChange:
						ret = new ParentChildRelationChange(
							MakeTrigger(elt),
							objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId)),
							null,
							newParentObjectId: objectIdsPool.Intern(Attr(elt, SC.Attr_NewParentObjectId)),
							isWeak: (Attr(elt, SC.Attr_IsWeak) ?? "0") == "1");
						break;
				}
				if (ret != null)
				{
					ret.Tags = tagsPool.Intern(
						new HashSet<string>((Attr(elt, SC.Attr_Tags) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
					yield return ret;
				}
			}
		}

		static ValueType ToValueType(string str)
		{
			if (str == valueTypeReferenceStr)
				return ValueType.Reference;
			else if (str == valueTypeThreadReferenceStr)
				return ValueType.ThreadReference;
			else if (str == valueTypeUserHashStr)
				return ValueType.UserHash;
			return ValueType.Scalar;
		}

		object MakeTrigger(XElement e)
		{
			if (triggerDeserializer != null)
				return triggerDeserializer(e);
			return null;
		}

		static string Attr(XElement e, string name)
		{
			var attr = e.Attribute(name);
			return attr == null ? null : attr.Value;
		}

		class ObjectTypeInfoPoolComparer : IEqualityComparer<ObjectTypeInfo>
		{
			bool IEqualityComparer<ObjectTypeInfo>.Equals(ObjectTypeInfo x, ObjectTypeInfo y)
			{
				return ObjectTypeInfo.Equals(x, y);
			}

			int IEqualityComparer<ObjectTypeInfo>.GetHashCode(ObjectTypeInfo obj)
			{
				return obj.TypeName.GetHashCode();
			}
		};

		readonly Func<XElement, object> triggerDeserializer;
		static readonly string valueTypeReferenceStr = ValueType.Reference.ToString().ToLower();
		static readonly string valueTypeThreadReferenceStr = ValueType.ThreadReference.ToString().ToLower();
		static readonly string valueTypeUserHashStr = ValueType.UserHash.ToString().ToLower();
		readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
		readonly StringInternPool objectIdsPool = new StringInternPool();
		readonly StringInternPool propNamesPool = new StringInternPool();
		readonly InternPool<ObjectTypeInfo> objectInfoPool = new InternPool<ObjectTypeInfo>(new ObjectTypeInfoPoolComparer());
	}
}
