using sfa_chat_graph.Server.Utils;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.RDF.Models
{
	public class SparqlStarConverter : JsonConverter<SparqlStarObject>
	{
		private FrozenDictionary<string, int> _termMapping;

		public SparqlStarConverter(string[] terms)
		{
			_termMapping = Enumerable.Range(0, terms.Count()).ToFrozenDictionary(i => terms[i]);
		}

		private SparqlStarTerm ReadTerm(ref Utf8JsonReader reader, JsonSerializerOptions options, out string termName)
		{
			termName = reader.GetString();
			return JsonSerializer.Deserialize<SparqlStarTerm>(ref reader, options);
		}

		public override SparqlStarObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			reader.ReadAndCheck(JsonTokenType.StartObject);
			var array = new SparqlStarTerm[_termMapping.Count];
			while(reader.ReadAndCheck(JsonTokenType.EndObject, JsonTokenType.PropertyName) != JsonTokenType.EndObject)
			{
				var term = ReadTerm(ref reader, options, out var termName);
				array[_termMapping[termName]] = term;	
			}

			reader.ReadAndCheck(JsonTokenType.EndObject);
			return new SparqlStarObject(_termMapping, array); 
		}

		public override void Write(Utf8JsonWriter writer, SparqlStarObject value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			foreach (var ( key, index) in _termMapping)
			{
				writer.WritePropertyName(key);
				JsonSerializer.Serialize(writer, value[index], options);
			}
			writer.WriteEndObject();
		}
	}
}
