using OpenAI.Assistants;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace sfa_chat_graph.Server.Utils.Json
{
	public class SparqlResultSetConverter : JsonConverter<SparqlResultSet>
	{


		private static ISparqlResult ReadBinding(JsonElement element)
		{
			var res = new SparqlResult();
			foreach (var obj in element.EnumerateObject())
				res.SetValue(obj.Name, ConverterUtils.ReadNode(obj.Value));

			return res;
		}

		public override SparqlResultSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			var type = root.GetProperty("type").GetString();
			if (Enum.TryParse<SparqlResultsType>(type, true, out var resultsType) == false)
				throw new JsonException($"Unknown results type {type}");

			switch (resultsType)
			{
				case SparqlResultsType.Boolean:
					var result = root.GetProperty("result").GetBoolean();
					return new SparqlResultSet(result);

				case SparqlResultsType.Unknown:
					return new SparqlResultSet();

				case SparqlResultsType.VariableBindings:
					var list = new List<ISparqlResult>();
					var results = root.GetProperty("results").GetProperty("bindings");
					foreach (var element in results.EnumerateArray())
						list.Add(ReadBinding(element));

					return new SparqlResultSet(list);
				
				default:
					throw new JsonException($"Unknown results type {type}.");	
			}
		}

		private static void WriteResult(Utf8JsonWriter writer, ISparqlResult result)
		{
			writer.WriteStartObject();
			foreach (var (key, node) in result)
			{
				writer.WritePropertyName(key);
				ConverterUtils.WriteNode(writer, node);
			}
			writer.WriteEndObject();
		}

		public override void Write(Utf8JsonWriter writer, SparqlResultSet value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("type");
			writer.WriteStringValue(value.ResultsType.ToString());
			if (value.ResultsType == SparqlResultsType.Boolean)
			{
				writer.WritePropertyName("result");
				writer.WriteBooleanValue(value.Result);
			}
			else
			{
				writer.WritePropertyName("head");
				JsonSerializer.Serialize(writer, value.Variables, options);
				writer.WriteStartObject("results");
				writer.WriteStartArray("bindings");
				foreach (var result in value.Results)
					WriteResult(writer, result);
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
			writer.WriteEndObject();
		}
	}
}
