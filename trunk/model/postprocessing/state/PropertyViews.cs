
using ICSharpCode.SharpZipLib.Zip;

namespace LogJoint.Postprocessing.StateInspector
{
    public class PropertyViewBase
    {
        public virtual object GetTrigger() { return null; }
        public virtual bool IsLink() { return false; }
        public IInspectedObject InspectedObject { get; private set; }

        public PropertyViewBase(IInspectedObject obj)
        {
            this.InspectedObject = obj;
        }

        public virtual string ToClipboardString() { return ToString(); }
    };

    public class SimplePropertyView : PropertyViewBase
    {
        protected object value;

        public SimplePropertyView(IInspectedObject obj, object value) : base(obj)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value?.ToString() ?? "";
        }
    };

    public class IdPropertyView : SimplePropertyView
    {
        public int ObjectNr;

        public IdPropertyView(IInspectedObject obj, object value) : base(obj, value) { }

        public override string ToString()
        {
            var str = value.ToString();
            str = InspectedObject.Owner.TryGetDisplayName(str, out var displayName) ? displayName : str;
            if (ObjectNr != 0)
                return string.Format("{0} (#{1})", str, ObjectNr);
            else
                return str;
        }
    }

    public class PropertyChangeView : PropertyViewBase
    {
        public readonly StateInspectorEvent Change;
        public readonly DisplayMode Mode;
        public readonly IInspectedObject Object;

        public enum DisplayMode
        {
            Date,
            Value,
            Reference,
            ThreadReference
        };

        public PropertyChangeView(IInspectedObject obj, StateInspectorEvent change, DisplayMode mode) : base(obj)
        {
            this.Change = change;
            this.Object = obj;
            this.Mode = mode;
        }

        public override string ToString()
        {
            if (Change == null)
                return "";

            if (Mode == DisplayMode.Value || Mode == DisplayMode.ThreadReference)
            {
                return ((PropertyChange)Change.OriginalEvent).Value ?? "";
            }
            if (Mode == DisplayMode.Reference)
            {
                var id = ((PropertyChange)Change.OriginalEvent).Value ?? "";
                return Object.Owner.TryGetDisplayName(id, out var displayName) ? displayName : id;
            }
            if (Mode == DisplayMode.Date)
            {
                if (Change.Output.LogSource.IsDisposed)
                    return "";
                return Change.Trigger.Timestamp.Adjust(Change.Output.LogSource.TimeOffsets).ToString();
            }
            return "";
        }

        public override object GetTrigger() { return Change; }

        public override bool IsLink()
        {
            return Mode == DisplayMode.Reference || Mode == DisplayMode.ThreadReference;
        }

        public override string ToClipboardString()
        {
            return ToString();
        }
    };

    public class SourceReferencePropertyView : PropertyViewBase
    {
        readonly ILogSource ls;

        public SourceReferencePropertyView(IInspectedObject obj, ILogSource ls) : base(obj)
        {
            this.ls = ls;
        }

        public override bool IsLink()
        {
            return true;
        }

        public override string ToString()
        {
            var s = ls.GetShortDisplayNameWithAnnotation();
            if (s.Length > 40)
                s = s.Substring(0, 40) + "...";
            return s;
        }

        public override object GetTrigger()
        {
            return ls;
        }
    }
}
