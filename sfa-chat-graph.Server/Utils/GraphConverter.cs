using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Utils
{
	public class GraphConverter : JsonConverter<IGraph>
	{
		public override IGraph Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
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

		private static void WriteNode(Utf8JsonWriter writer, INode node)
		{
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

		public override void Write(Utf8JsonWriter writer, IGraph value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("head");
			writer.WriteRawValue("""["s", "p", "o"]""");
			writer.WritePropertyName("results");
			writer.WriteStartObject();
			writer.WritePropertyName("bindings");
			writer.WriteStartArray();
			foreach (var triple in value.Triples)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("s");
				WriteNode(writer, triple.Subject);
				writer.WritePropertyName("p");
				WriteNode(writer, triple.Predicate);
				writer.WritePropertyName("o");
				WriteNode(writer, triple.Object);
				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.WriteEndObject();
		}
	}
}
