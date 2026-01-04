using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.StateInspector
{
    public class InspectedObject : IInspectedObject
    {
        public InspectedObject(IStateInspectorOutputsGroup owner, string id)
        {
            this.owner = owner;
            this.id = id;
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
                comment =
                    commentPropertyName != null ? FindStaticPropertyValue(commentPropertyName) : "";
                return comment;
            }
        }

        string IInspectedObject.Description
        {
            get
            {
                if (description != null)
                    return description;
                description =
                    descriptionPropertyName != null ? FindStaticPropertyValue(descriptionPropertyName) : "";
                return description;
            }
        }

        IEnumerable<IInspectedObject> IInspectedObject.Children
        {
            get { return children; }
        }

        IEnumerable<StateInspectorEvent> IInspectedObject.StateChangeHistory
        {
            get
            {
                if (descriptionPropertyName == null)
                    return history;
                return
                    from change in history
                    let pc = change.OriginalEvent as PropertyChange
                    where pc == null || pc.PropertyName != descriptionPropertyName
                    select change;
            }
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
            if (lastPC.ValueType == ValueType.Reference && owner.TryGetDisplayName(lastPC.Value, out var displayName))
                return displayName;
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
            descriptionPropertyName = cevt.ObjectType.DescriptionPropertyName;
            isTimeless = cevt.ObjectType.IsTimeless;
            displayName = cevt.DisplayName;
        }

        bool IInspectedObject.SetDeletionEvent(StateInspectorEvent evt)
        {
            if (deletion == null)
            {
                deletion = evt;
                return true;
            }
            return false;
        }

        void IInspectedObject.AddStateChangeEvent(StateInspectorEvent evt)
        {
            history.Add(evt);
        }


        IEnumerable<KeyValuePair<string, PropertyViewBase>> IInspectedObject.GetCurrentProperties(FocusedMessageEventsRange focusedMessageEqualRange)
        {
            yield return new KeyValuePair<string, PropertyViewBase>("id", new IdPropertyView(this, id));
            if (creation != null && !isTimeless)
                yield return new KeyValuePair<string, PropertyViewBase>("created at",
                    new PropertyChangeView(this, creation, PropertyChangeView.DisplayMode.Date));
            if (deletion != null && !isTimeless)
                yield return new KeyValuePair<string, PropertyViewBase>("deleted at",
                    new PropertyChangeView(this, deletion, PropertyChangeView.DisplayMode.Date));
            if (focusedMessageEqualRange.EqualRange == null)
                yield break;
            var dynamicProps = new Dictionary<string, PropertyViewBase>();
            foreach (var change in history
                .TakeWhile(e => isTimeless || e.Index < focusedMessageEqualRange.EqualRange.Item2)
                .Select(e => new { ChangeEvt = e.OriginalEvent as PropertyChange, StateInspectorEvt = e })
                .Where(e => e.ChangeEvt != null)
                .Where(e => descriptionPropertyName == null || e.ChangeEvt.PropertyName != descriptionPropertyName))
            {
                dynamicProps[change.ChangeEvt.PropertyName] =
                    new PropertyChangeView(this, change.StateInspectorEvt, ToPropDisplayMode(change.ChangeEvt.ValueType));
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

        static PropertyChangeView.DisplayMode ToPropDisplayMode(ValueType propValueType)
        {
            if (propValueType == ValueType.Reference)
                return PropertyChangeView.DisplayMode.Reference;
            if (propValueType == ValueType.ThreadReference)
                return PropertyChangeView.DisplayMode.ThreadReference;
            return PropertyChangeView.DisplayMode.Value;
        }

        private string FindStaticPropertyValue(string name)
        {
            var query =
                from e in history
                let pc = e.OriginalEvent as PropertyChange
                where pc != null
                where pc.PropertyName == name
                select pc;
            var change = query.FirstOrDefault();
            if (change == null)
                return "";
            return change.Value;
        }

        readonly IStateInspectorOutputsGroup owner;
        readonly string id;
        readonly HashSet<IInspectedObject> children = new HashSet<IInspectedObject>();
        List<StateInspectorEvent> history = new List<StateInspectorEvent>();
        string commentPropertyName;
        string comment;
        string primaryPropertyName;
        string descriptionPropertyName;
        string description;
        bool isTimeless;
        string displayName;
        IInspectedObject parent;
        StateInspectorEvent creation;
        StateInspectorEvent deletion;
    };
}
