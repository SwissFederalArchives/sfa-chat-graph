using System.Collections.Frozen;
using System.Text.Json;
using VDS.RDF;

namespace sfa_chat_graph.Server.Utils
{
	public static class ConverterUtils
	{
		public static void WriteBlankNode(Utf8JsonWriter writer, BlankNode node)
		{
			writer.WritePropertyName("type");
			writer.WriteStringValue("bnode");
			writer.WritePropertyName("value");
			writer.WriteStringValue(node.InternalID);
		}

		public static void WriteUriNode(Utf8JsonWriter writer, UriNode node)
		{
			writer.WritePropertyName("type");
			writer.WriteStringValue("uri");
			writer.WritePropertyName("value");
			writer.WriteStringValue(node.Uri.ToString());
		}

		public static void WriteLiteralNode(Utf8JsonWriter writer, LiteralNode node)
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

		public static INode ReadNode(JsonElement element)
		{
			if(element.ValueKind == JsonValueKind.Null)
				return null;

			var type = element.GetProperty("type").GetString();
			var value = element.GetProperty("value").GetString();
			switch (type)
			{
				case "uri":
					return new UriNode(new Uri(value));

				case "literal":
					if (element.TryGetProperty("datatype", out var datatypeValue) && datatypeValue.ValueKind == JsonValueKind.String && Uri.TryCreate(datatypeValue.GetString(), UriKind.RelativeOrAbsolute, out var datatypeUri))
						return new LiteralNode(value, datatypeUri);

					if (element.TryGetProperty("xml:lang", out var langValue) && langValue.ValueKind == JsonValueKind.String)
						return new LiteralNode(value, langValue.GetString());

					return new LiteralNode(value);

				case "bnode":
					return new BlankNode(value);

				default:
					throw new JsonException($"Unknown node type {type}");
			}
		}

		public static INode ReadNode(ref Utf8JsonReader reader)
		{
			string type = null;
			string value = null;
			string dataType = null;
			string lang = null;

			if(reader.TokenType == JsonTokenType.Null)
				return null;

			reader.ReadAndCheck(JsonTokenType.StartObject);
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;

				if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
				{
					var propertyName = reader.GetString();
					if (reader.Read() && (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.Null))
					{
						switch (propertyName)
						{
							case "type":
								type = reader.GetString();
								break;

							case "value":
								value = reader.GetString();
								break;

							case "datatype":
								dataType = reader.GetString();
								break;

							case "xml:lang":
								lang = reader.GetString();
								break;
						}
					}
				}
			}

			if (type == null || value == null)
				throw new JsonException("Missing type or value in node");

			switch (type)
			{
				case "uri":
					return new UriNode(new Uri(value));
				
				case "literal":
					if (dataType != null && Uri.TryCreate(dataType, UriKind.RelativeOrAbsolute, out var datatypeUri))
						return new LiteralNode(value, datatypeUri);
					
					if (lang != null)
						return new LiteralNode(value, lang);
				
					return new LiteralNode(value);
				
				case "bnode":
					return new BlankNode(value);
			
				default:
					throw new JsonException($"Unknown node type {type}");
			}
		}

		public static void WriteNode(Utf8JsonWriter writer, INode node)
		{
			if(node == null)
			{
				writer.WriteNullValue();
				return;
			}

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
	}
}
