using OpenAI.Assistants;
using SfaChatGraph.Server.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace sfa_chat_graph.Server.Utils
{
	public class SparqlResultSetConverter : JsonConverter<SparqlResultSet>
	{
		private static INode ReadNode(JsonElement element)
		{
			var type = element.GetProperty("type").GetString();
			var value = element.GetProperty("value").GetString();
			switch (type)
			{
				case "uri":
					return new UriNode(new Uri(value));

				case "literal":
					if(element.TryGetProperty("datatype", out var datatypeValue) && datatypeValue.ValueKind == JsonValueKind.String && Uri.TryCreate(datatypeValue.GetString(), UriKind.RelativeOrAbsolute, out var datatypeUri))
						return new LiteralNode(value, datatypeUri);

					if(element.TryGetProperty("xml:lang", out var langValue) && langValue.ValueKind == JsonValueKind.String)
						return new LiteralNode(value, langValue.GetString());

					return new LiteralNode(value);

				case "bnode":
					return new BlankNode(value);

				default:
					throw new JsonException($"Unknown node type {type}");
			}
		}

		private static ISparqlResult ReadBinding(JsonElement element)
		{
			var res = new SparqlResult();
			foreach (var obj in element.EnumerateObject())
				res.SetValue(obj.Name, ReadNode(obj.Value));
			
			return res;
		}

		public override SparqlResultSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			var type = root.GetProperty("type").GetString();
			if (Enum.TryParse<SparqlResultsType>(type, true, out var resultsType) == false)
				throw new JsonException($"Unknown results type {type}");

			if (resultsType == SparqlResultsType.Boolean)
			{
				var result = root.GetProperty("result").GetBoolean();
				return new SparqlResultSet(result);
			}
			else
			{
				var set = new SparqlResultSet();
				var results = root.GetProperty("results").GetProperty("bindings");
				foreach (var element in results.EnumerateArray())
					set.Results.Add(ReadBinding(element));
				
				return set;
			}

		}

		private static void WriteBlankNode(Utf8JsonWriter writer, BlankNode node)
		{
			writer.WritePropertyName("type");
			writer.WriteStringValue("bnode");
			writer.WritePropertyName("value");
			writer.WriteStringValue(node.InternalID);
		}

		private static void WriteUriNode(Utf8JsonWriter writer, UriNode node)
		{
			writer.WritePropertyName("type");
			writer.WriteStringValue("uri");
			writer.WritePropertyName("value");
			writer.WriteStringValue(node.Uri.ToString());
		}

		private static void WriteLiteralNode(Utf8JsonWriter writer, LiteralNode node)
		{
			writer.WritePropertyName("type");
			writer.WriteStringValue("literal");
			writer.WritePropertyName("value");
			writer.WriteStringValue(node.Value);

			if (node.DataType != null)
			{
				writer.WritePropertyName("datatype");
				writer.WriteStringValue(node.DataType.ToString());
			}

			if (string.IsNullOrEmpty(node.Language) == false)
			{
				writer.WritePropertyName("xml:lang");
				writer.WriteStringValue(node.Language);
			}
		}

		private static void WriteResult(Utf8JsonWriter writer, ISparqlResult result)
		{
			writer.WriteStartObject();
			foreach (var (key, node) in result)
			{
				writer.WritePropertyName(key);
				writer.WriteStartObject();

				switch (node)
				{
					case LiteralNode literalNode:
						WriteLiteralNode(writer, literalNode);
						break;

					case UriNode uriNode:
						WriteUriNode(writer, uriNode);
						break;

					case BlankNode blankNode:
						WriteBlankNode(writer, blankNode);
						break;
				}

				writer.WriteEndObject();
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
