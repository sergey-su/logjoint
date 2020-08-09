using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.Google
{
	public class TextProtoParser
	{
		enum State
		{
			Value, ScalarValue, RepeatedValue, EscapedScalarValue, Object, CustomFormattedValue
		};
		class StackEntry
		{
			public State state;
			public JObject obj;
			public JToken value;
			public StringBuilder scalarValue;
			public string propName;
			public int customFormattedValueBracketCount = 0;
		};
		readonly Stack<StackEntry> stateStack = new Stack<StackEntry>();
		readonly static Regex propNameRe = new Regex(@"^([\w\-]+)((:\s*)|(\s{1,}))", RegexOptions.Compiled);
		readonly static Regex typeNameRe = new Regex(@"^\[([^\]]+)\]\s*", RegexOptions.Compiled);

		StackEntry Push(State state, string propName = null)
		{
			var e = new StackEntry() { state = state, propName = propName };
			if (state == State.Object)
				e.obj = new JObject();
			else if (state == State.ScalarValue || state == State.EscapedScalarValue || state == State.CustomFormattedValue)
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
			else if (entry.state == State.ScalarValue || entry.state == State.EscapedScalarValue || entry.state == State.CustomFormattedValue)
				top.value = new JValue(entry.scalarValue.ToString().Trim());
			else if (entry.state == State.Value)
			{
				if (entry.propName != null && top.obj.Property(entry.propName) == null)
					top.obj.Add(entry.propName, entry.value);
			}
			else if (entry.state == State.RepeatedValue)
				if (entry.propName != null)
				{
					var arrayProp = top.obj.Property(entry.propName);
					JArray array;
					if (arrayProp == null)
						top.obj.Add(entry.propName, array = new JArray());
					else
						array = arrayProp.Value as JArray;
					if (array != null)
						array.Add(entry.value);
				}
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
					case State.RepeatedValue:
						if (c == '{')
							Push(State.Object);
						else if (c == '"')
							Push(State.EscapedScalarValue);
						else if (c == '[')
							Push(State.CustomFormattedValue);
						else
						{
							posIncrement = 0;
							Push(State.ScalarValue);
						}
						break;
					case State.ScalarValue:
						if (state.state == State.ScalarValue && c == ' ')
						{
							Pop();
							Pop();
							posIncrement = 0;
						}
						else
						{
							state.scalarValue.Append(c);
						}
						break;
					case State.EscapedScalarValue:
						if (state.state == State.EscapedScalarValue && c == '"')
						{
							Pop();
							Pop();
						}
						else
						{
							state.scalarValue.Append(c);
						}
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
							Push(m.Groups[3].Length > 0 ? State.Value : State.RepeatedValue, m.Groups[1].Value.Trim());
						}
						else if ((m = typeNameRe.Match(value, pos, value.Length - pos)).Success)
						{
							posIncrement = m.Length;
							state.obj.Add("type", m.Groups[1].Value);
							Push(State.Value, "value");
						}
						else if (!char.IsWhiteSpace(c))
						{
							Push(State.Value, "value");
							posIncrement = 0;
						}
						break;
					case State.CustomFormattedValue:
						if (c == ']')
						{
							if (state.customFormattedValueBracketCount == 0)
							{
								Pop();
								Pop();
								break;
							}
							state.customFormattedValueBracketCount--;
						}
						else if (c == '[')
							state.customFormattedValueBracketCount++;
						state.scalarValue.Append(c);
						break;
				}
				pos += posIncrement;
			}
			while (stateStack.Count > 1) Pop();
			return topValue.value;
		}

		public static JToken Parse(string value)
		{
			return (new TextProtoParser()).ParseImpl(value);
		}
	}
}
