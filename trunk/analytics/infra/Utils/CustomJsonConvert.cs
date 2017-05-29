using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace LogJoint.Analytics
{
	// Force lists to be serialized on a single line (more readable, less disk space spent on spaces)
	internal class SingleLineConverter : JsonConverter
	{
		private Type[] Types;

		public SingleLineConverter(params Type[] types)
		{
			Types = types;
		}

		public override bool CanConvert(Type objectType)
		{
			return Array.IndexOf(Types, objectType) >= 0;
		}

		public override bool CanRead { get { return false; } }

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.Formatting = Formatting.None;
			var representation = JToken.FromObject(value);
			representation.WriteTo(writer);
			writer.Formatting = Formatting.Indented;
		}
	}

	// Sort the fields alphabetically for consistent serialization (required to make the test fixture reproducible)
	public class OrderedContractResolver : DefaultContractResolver
	{
		protected override System.Collections.Generic.IList<JsonProperty> CreateProperties(System.Type type, MemberSerialization memberSerialization)
		{
			return base.CreateProperties(type, memberSerialization).OrderBy(prop => prop.PropertyName).ToList();
		}
	}

	public class CustomJsonConvert
	{
		private bool NumericEnums;

		public CustomJsonConvert(bool numericEnums = false)
		{
			NumericEnums = numericEnums;
		}

		private IEnumerable<JsonConverter> IterConverters()
		{
			yield return new SingleLineConverter(typeof(List<double>), typeof(double[]));

			if (!NumericEnums)
			{
				yield return new StringEnumConverter();
			}
		}

		private JsonSerializer CreateSerializer()
		{
			var serializer = new JsonSerializer();
			serializer.Formatting = Formatting.Indented;
			serializer.ContractResolver = new OrderedContractResolver();
			
			foreach (var converter in IterConverters())
			{
				serializer.Converters.Add(converter);
			}

			return serializer;
		}

		public string SerializeObject(object value)
		{
			using (var stringWriter = new StringWriter())
			{
				SerializeObject(stringWriter, value);
				return stringWriter.ToString();
			}
		}

		public void SerializeObject(TextWriter writer, object value)
		{
			using (var jsonTextWriter = new JsonTextWriter(writer))
			{
				var serializer = CreateSerializer();
				serializer.Serialize(jsonTextWriter, value);
			}
		}

		public void SerializeObjectToFile(string jsonPath, object value)
		{
			using (var writer = new System.IO.StreamWriter(jsonPath))
			{
				SerializeObject(writer, value);
			}
		}

		public static T DeserializeObject<T>(string value)
		{
			return JsonConvert.DeserializeObject<T>(value, new StringEnumConverter());
		}

		public static T DeserializeObject<T>(StreamReader reader)
		{
			var serializer = new JsonSerializer();
			using (var jsonTextReader = new JsonTextReader(reader))
			{
				return serializer.Deserialize<T>(jsonTextReader);
			}
		}

		public static T DeserializeObjectFromFile<T>(string jsonPath)
		{
			using (var reader = new System.IO.StreamReader(jsonPath)) 
			{
				return DeserializeObject<T>(reader);
			}
		}
	}
}
