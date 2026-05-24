using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using SC = LogJoint.Postprocessing.StateInspector.SerializationCommon;

namespace LogJoint.Postprocessing.StateInspector
{
    public class EventsDeserializer
    {
        public EventsDeserializer(Func<XElement, object>? triggerDeserializer = null)
        {
            this.triggerDeserializer = triggerDeserializer;
        }

        public bool TryDeserialize(XElement elt, [MaybeNullWhen(false)] out Event ret)
        {
            ret = null;
            string? objectId = null;
            string? propertyName = null;
            string? objectType = null;
            switch (elt.Name.LocalName)
            {
                case SC.Elt_ObjectCreation:
                    objectId = objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId));
                    objectType = Attr(elt, SC.Attr_ObjectType);
                    if (objectId != null && objectType != null)
                        ret = new ObjectCreation(
                            MakeTrigger(elt),
                            objectId,
                            objectInfoPool.Intern(new ObjectTypeInfo(
                                objectType,
                                new ObjectTypeInfo.Options
                                {
                                    CommentPropertyName = Attr(elt, SC.Attr_CommentPropertyName),
                                    PrimaryPropertyName = Attr(elt, SC.Attr_PrimaryPropertyName),
                                    IsTimeless = (Attr(elt, SC.Attr_IsTimeless) ?? "0") == "1",
                                    DescriptionPropertyName = Attr(elt, SC.Attr_DescriptionPropertyName)
                                }
                            )),
                            isWeak: (Attr(elt, SC.Attr_IsWeak) ?? "0") == "1",
                            displayName: Attr(elt, SC.Attr_DisplayNamePropertyName)
                        );
                    break;
                case SC.Elt_ObjectDeletion:
                    objectId = objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId));
                    if (objectId != null)
                        ret = new ObjectDeletion(MakeTrigger(elt), objectId, null);
                    break;
                case SC.Elt_PropertyChange:
                    objectId = objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId));
                    propertyName = propNamesPool.Intern(Attr(elt, SC.Attr_PropertyName));
                    if (objectId != null && propertyName != null)
                        ret = new PropertyChange(
                            MakeTrigger(elt),
                            objectId,
                            null,
                            propertyName: propertyName,
                            value: Attr(elt, SC.Attr_Value) ?? "",
                            oldValue: Attr(elt, SC.Attr_OldValue),
                            valueType: ToValueType(Attr(elt, SC.Attr_ValueType)));
                    break;
                case SC.Elt_ParentChildRelationChange:
                    objectId = objectIdsPool.Intern(Attr(elt, SC.Attr_ObjectId));
                    if (objectId != null)
                        ret = new ParentChildRelationChange(
                            MakeTrigger(elt),
                            objectId,
                            null,
                            newParentObjectId: objectIdsPool.Intern(Attr(elt, SC.Attr_NewParentObjectId)),
                            isWeak: (Attr(elt, SC.Attr_IsWeak) ?? "0") == "1");
                    break;
            }
            if (ret != null)
            {
                ret.Tags = tagsPool.Intern(
                    new HashSet<string>((Attr(elt, SC.Attr_Tags) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
            }
            return ret != null;
        }

        static ValueType ToValueType(string? str)
        {
            if (str == valueTypeReferenceStr)
                return ValueType.Reference;
            else if (str == valueTypeThreadReferenceStr)
                return ValueType.ThreadReference;
            return ValueType.Scalar;
        }

        object? MakeTrigger(XElement e)
        {
            if (triggerDeserializer != null)
                return triggerDeserializer(e);
            return null;
        }

        static string? Attr(XElement e, string name)
        {
            var attr = e.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        class ObjectTypeInfoPoolComparer : IEqualityComparer<ObjectTypeInfo>
        {
            bool IEqualityComparer<ObjectTypeInfo>.Equals(ObjectTypeInfo? x, ObjectTypeInfo? y)
            {
                return ObjectTypeInfo.Equals(x, y);
            }

            int IEqualityComparer<ObjectTypeInfo>.GetHashCode(ObjectTypeInfo obj)
            {
                return obj.TypeName.GetHashCode();
            }
        };

        readonly Func<XElement, object>? triggerDeserializer;
        static readonly string valueTypeReferenceStr = ValueType.Reference.ToString().ToLower();
        static readonly string valueTypeThreadReferenceStr = ValueType.ThreadReference.ToString().ToLower();
        readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
        readonly StringInternPool objectIdsPool = new StringInternPool();
        readonly StringInternPool propNamesPool = new StringInternPool();
        readonly InternPool<ObjectTypeInfo> objectInfoPool = new InternPool<ObjectTypeInfo>(new ObjectTypeInfoPoolComparer());
    }
}
