using System.Linq;
using System.Collections.Generic;
using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;

namespace LogJoint.Postprocessing.StateInspector
{
	public class InspectedObject: IInspectedObject
	{
		public InspectedObject(IStateInspectorOutputsGroup owner, string id, IUserNamesProvider shortNames)
		{
			this.owner = owner;
			this.id = id;
			this.shortNames = shortNames;
		}

		IStateInspectorOutputsGroup IInspectedObject.Owner { get { return owner; } }

		IInspectedObject IInspectedObject.Parent { get { return parent; } }

		string IInspectedObject.Id
		{
			get { return id; }
		}

		string IInspectedObject.DisplayName 
		{
			get { return displayName ?? id; }
		}

		string IInspectedObject.Comment
		{
			get
			{
				if (comment != null)
					return comment;
				comment = "";
				if (commentPropertyName != null)
				{
					var query =
						from e in history
						let pc = e.OriginalEvent as PropertyChange
						where pc != null
						where pc.PropertyName == commentPropertyName
						select pc;
					var change = query.FirstOrDefault();
					if (change == null)
						return "";
					if (change.ValueType == ValueType.UserHash)
						comment = shortNames.GetShortNameForUserHash(change.Value);
					else
						comment = change.Value;
				};
				return comment;
			}
		}

		IEnumerable<IInspectedObject> IInspectedObject.Children
		{
			get { return children; }
		}

		IEnumerable<StateInspectorEvent> IInspectedObject.StateChangeHistory
		{
			get { return history; }
		}

		bool IInspectedObject.IsTimeless
		{
			get { return isTimeless; }
		}

		StateInspectorEvent IInspectedObject.CreationEvent { get { return creation; } }

		string IInspectedObject.GetCurrentPrimaryPropertyValue(FocusedMessageEventsRange focusedMessage)
		{
			if (primaryPropertyName == null)
				return null;
			var query =
				from change in history
				let pc = change.OriginalEvent as PropertyChange
				where pc != null
				where pc.PropertyName == primaryPropertyName
				select new { idx = change.Index, pc = pc };
			var lastPC = query
				.TakeWhile(pc => isTimeless || pc.idx < focusedMessage.EqualRange.Item2)
				.Select(pc => pc.pc)
				.LastOrDefault();
			if (lastPC == null)
				return null;
			if (lastPC.ValueType == ValueType.UserHash)
				return shortNames.GetShortNameForUserHash(lastPC.Value);
			return lastPC.Value;
		}

		IEnumerable<ILogSource> IInspectedObject.EnumInvolvedLogSources()
		{
			return owner.Outputs.Select(x => x.LogSource);
		}

		void IInspectedObject.SetParent(IInspectedObject value)
		{
			this.parent = value;
		}

		void IInspectedObject.RemoveChild(IInspectedObject child)
		{
			children.Remove(child);
		}

		void IInspectedObject.AddChild(IInspectedObject child)
		{
			children.Add(child);
		}

		void IInspectedObject.SetCreationEvent(StateInspectorEvent evt)
		{
			if (creation != null)
				return;
			creation = evt;
			var cevt = (ObjectCreation)evt.OriginalEvent;
			commentPropertyName = cevt.ObjectType.CommentPropertyName;
			primaryPropertyName = cevt.ObjectType.PrimaryPropertyName;
			isTimeless = cevt.ObjectType.IsTimeless;
			displayName = cevt.DisplayName;
		}

		void IInspectedObject.SetDeletionEvent(StateInspectorEvent evt)
		{
			if (deletion == null)
				deletion = evt;
		}

		void IInspectedObject.AddStateChangeEvent(StateInspectorEvent evt)
		{
			history.Add(evt);
		}
	

		IEnumerable<KeyValuePair<string, PropertyViewBase>> IInspectedObject.GetCurrentProperties(FocusedMessageEventsRange focusedMessageEqualRange)
		{
			yield return new KeyValuePair<string, PropertyViewBase>("id", new IdPropertyView(this, id));
			if (parent == null)
				yield return new KeyValuePair<string, PropertyViewBase>("(log source)",
					new SourceReferencePropertyView(this));
			if (creation != null && !isTimeless)
				yield return new KeyValuePair<string, PropertyViewBase>("created at",
					new PropertyChangeView(this, creation, PropertyChangeView.DisplayMode.Date, shortNames));
			if (deletion != null && !isTimeless)
				yield return new KeyValuePair<string, PropertyViewBase>("deleted at",
					new PropertyChangeView(this, deletion, PropertyChangeView.DisplayMode.Date, shortNames));
			if (focusedMessageEqualRange.EqualRange == null)
				yield break;
			var dynamicProps = new Dictionary<string, PropertyViewBase>();
			foreach (var change in history
				.TakeWhile(e => isTimeless || e.Index < focusedMessageEqualRange.EqualRange.Item2)
				.Select(e => new { ChangeEvt = e.OriginalEvent as PropertyChange, StateInspectorEvt = e })
				.Where(e => e.ChangeEvt != null))
			{
				dynamicProps[change.ChangeEvt.PropertyName] =
					new PropertyChangeView(this, change.StateInspectorEvt, ToPropDisplayMode(change.ChangeEvt.ValueType), shortNames);
			}
			foreach (var v in dynamicProps)
				yield return v;
		}

		InspectedObjectLiveStatus IInspectedObject.GetLiveStatus(FocusedMessageEventsRange focusedMessage)
		{
			return GetLiveStatusInternal(focusedMessage);
		}

		private InspectedObjectLiveStatus GetLiveStatusInternal(FocusedMessageEventsRange focusedMessage)
		{
			if (isTimeless)
				return InspectedObjectLiveStatus.NotCreatedYet;
			if (creation == null || focusedMessage.EqualRange == null)
				return InspectedObjectLiveStatus.NotCreatedYet;
			if (focusedMessage.EqualRange.Item2 <= creation.Index)
				return InspectedObjectLiveStatus.NotCreatedYet;
			if (deletion == null)
				return InspectedObjectLiveStatus.Alive;
			if (focusedMessage.EqualRange.Item1 > deletion.Index)
				return InspectedObjectLiveStatus.Deleted;
			return InspectedObjectLiveStatus.Alive;
		}

		static string ToString(InspectedObjectLiveStatus status)
		{
			switch (status)
			{
				case InspectedObjectLiveStatus.Deleted:
					return "deleted";
				case InspectedObjectLiveStatus.Alive:
					return "alive";
				case InspectedObjectLiveStatus.NotCreatedYet:
					return "not created yet";
				default:
					return "";
			}
		}

		static PropertyChangeView.DisplayMode ToPropDisplayMode(ValueType propValueType)
		{
			if (propValueType == ValueType.Reference)
				return PropertyChangeView.DisplayMode.Reference;
			if (propValueType == ValueType.ThreadReference)
				return PropertyChangeView.DisplayMode.ThreadReference;
			if (propValueType == ValueType.UserHash)
				return PropertyChangeView.DisplayMode.UserHash;
			return PropertyChangeView.DisplayMode.Value;
		}

		readonly IStateInspectorOutputsGroup owner;
		readonly string id;
		readonly HashSet<IInspectedObject> children = new HashSet<IInspectedObject>();
		readonly IUserNamesProvider shortNames;
		List<StateInspectorEvent> history = new List<StateInspectorEvent>();
		string commentPropertyName;
		string comment;
		string primaryPropertyName;
		bool isTimeless;
		string displayName;
		IInspectedObject parent;
		StateInspectorEvent creation;
		StateInspectorEvent deletion;
	};
}
