
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.ChromeDebugLog
{
    public class JsonLikeStringParser
    {
        enum State
        {
            Value, ScalarValue, Object, Array
        };
        class StackEntry
        {
            public State state;
            public JObject obj;
            public JArray array;
            public JToken value;
            public StringBuilder scalarValue;
            public string propName;
        };
        readonly Stack<StackEntry> stateStack = new Stack<StackEntry>();
        readonly static Regex propNameRe = new Regex(@"^([\w\d\s\(\)\-]+)(:|->)\s*", RegexOptions.Compiled);

        StackEntry Push(State state, string propName = null)
        {
            var e = new StackEntry() { state = state, propName = propName };
            if (state == State.Object)
                e.obj = new JObject();
            else if (state == State.Array)
                e.array = new JArray();
            else if (state == State.ScalarValue)
                e.scalarValue = new StringBuilder();
            stateStack.Push(e);
            return e;
        }

        void Pop()
        {
            if (stateStack.Count == 0)
                return;
            var entry = stateStack.Pop();
            var top = stateStack.Count > 0 ? stateStack.Peek() : null;
            if (entry.state == State.Object)
                top.value = entry.obj;
            else if (entry.state == State.Array)
                top.value = entry.array;
            else if (entry.state == State.ScalarValue)
                top.value = new JValue(entry.scalarValue.ToString().Trim());
            else if (entry.state == State.Value)
                if (entry.propName != null && top.obj.Property(entry.propName) == null)
                    top.obj.Add(entry.propName, entry.value);
                else if (top?.array != null)
                    top.array.Add(entry.value);
        }

        JToken ParseImpl(string value)
        {
            var topValue = Push(State.Value);
            for (int pos = 0; pos < value.Length && stateStack.Count > 0;)
            {
                char c = value[pos];
                int posIncrement = 1;
                var state = stateStack.Peek();
                switch (state.state)
                {
                    case State.Value:
                        if (c == '{')
                            Push(State.Object);
                        else if (c == '[')
                            Push(State.Array);
                        else
                        {
                            posIncrement = 0;
                            Push(State.ScalarValue);
                        }
                        break;
                    case State.ScalarValue:
                        if (c == ',' || c == '}' || c == ']')
                        {
                            Pop();
                            Pop();
                            posIncrement = 0;
                        }
                        else
                            state.scalarValue.Append(c);
                        break;
                    case State.Object:
                        Match m;
                        if (c == '}')
                        {
                            Pop();
                            Pop();
                        }
                        else if ((m = propNameRe.Match(value, pos, value.Length - pos)).Success)
                        {
                            posIncrement = m.Length;
                            Push(State.Value, m.Groups[1].Value.Trim());
                        }
                        break;
                    case State.Array:
                        if (c == ']')
                        {
                            Pop();
                            Pop();
                        }
                        else if (c == ',' || char.IsWhiteSpace(c))
                        { }
                        else
                        {
                            posIncrement = 0;
                            Push(State.Value);
                        }
                        break;
                }
                pos += posIncrement;
            }
            return topValue.value;
        }

        public static JToken Parse(string value)
        {
            return (new JsonLikeStringParser()).ParseImpl(value);
        }
    };
}
