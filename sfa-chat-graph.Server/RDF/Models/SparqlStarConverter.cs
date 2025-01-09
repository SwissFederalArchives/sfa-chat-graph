using Lucene.Net.Index;
using SfaChatGraph.Server.Utils;
using System.Collections.Frozen;
using System.Dynamic;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.RDF.Models
{
	public class SparqlStarConverter : JsonConverter<SparqlStarResult>
	{

		private SparqlStarTerm ReadTerm(ref Utf8JsonReader reader, JsonSerializerOptions options, out string termName)
		{
			termName = reader.GetString();
			return JsonSerializer.Deserialize<SparqlStarTerm>(ref reader, options);
		}

		private SparqlStarObject ReadObject(ref Utf8JsonReader reader, FrozenDictionary<string, int> termMapping, JsonSerializerOptions options)
		{
			// reader.Check(JsonTokenType.StartObject);
			var array = new SparqlStarTerm[termMapping.Count];
			while (reader.ReadAndCheck(JsonTokenType.PropertyName, JsonTokenType.EndObject) != JsonTokenType.EndObject)
			{
				var term = ReadTerm(ref reader, options, out var termName);
				array[termMapping[termName]] = term;
			}

			return new SparqlStarObject(termMapping, array);
		}

		public override SparqlStarResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.ReadNamedObject() != "head")
				throw new InvalidOperationException("Expected head object");

			var head = JsonSerializer.Deserialize<SparqlStarHead>(ref reader, options);

			var mapping = head.Vars.Enumerate().ToFrozenDictionary(i => i.item, i => i.index);
			if(reader.ReadNamedObject() != "results")
				throw new InvalidOperationException("Expected results object");

			reader.Check(JsonTokenType.StartObject);
			if (reader.ReadNamedArray() != "bindings")
				throw new InvalidOperationException("Expected bindings array");

			var results = new List<SparqlStarObject>();
			while (reader.ReadAndCheck(JsonTokenType.StartObject, JsonTokenType.EndArray) != JsonTokenType.EndArray)
			{
				results.Add(ReadObject(ref reader, mapping, options));
			}

			reader.ReadAndCheck(JsonTokenType.EndObject);
			reader.ReadAndCheck(JsonTokenType.EndObject);
			return new SparqlStarResult(head, results.ToArray(), mapping);	
		}

		private void WriteObject(Utf8JsonWriter writer, SparqlStarObject value, FrozenDictionary<string, int> termMapping, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			foreach (var (key, index) in termMapping)
			{
				writer.WritePropertyName(key);
				JsonSerializer.Serialize(writer, value[index], options);
			}
			writer.WriteEndObject();
		}

		public override void Write(Utf8JsonWriter writer, SparqlStarResult value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("head");
			JsonSerializer.Serialize(writer, value.Head, options);
			writer.WriteStartObject("results");
			writer.WriteStartArray("bindings");
			foreach (var obj in value.Results)
				WriteObject(writer, obj, value.Mapping, options);

			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.WriteEndObject();
		}
	}
}
