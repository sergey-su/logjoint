using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics.StateInspector
{
	public class EventsStringifier: IEventsVisitor
	{
		public readonly StringBuilder Output = new StringBuilder();

		void IEventsVisitor.Visit(ObjectCreation objectCreation)
		{
			Output.AppendFormat("ObjectCreation: {0}", objectCreation.ObjectId);
		}

		void IEventsVisitor.Visit(ObjectDeletion objectDeletion)
		{
			Output.AppendFormat("ObjectDeletion: {0}", objectDeletion.ObjectId);
		}

		void IEventsVisitor.Visit(PropertyChange propertyChange)
		{
			Output.AppendFormat("PropertyChange: {0}.{1} {2} -> {3}", 
				propertyChange.ObjectId, propertyChange.PropertyName, propertyChange.OldValue ?? "", propertyChange.Value);
		}

		void IEventsVisitor.Visit(ParentChildRelationChange parentChildRelationChange)
		{
			Output.AppendFormat("ParentChildRelationChange: {0} became a child of {1}", 
				parentChildRelationChange.ObjectId, parentChildRelationChange.NewParentObjectId);
		}
	}
}
