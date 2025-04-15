using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Utils
{
	public class GraphConverter<T> : JsonConverter<T> where T : IGraph
	{

		private readonly Func<T> _supplier;

		public GraphConverter(Func<T> supplier = null)
		{
			_supplier = supplier ?? Activator.CreateInstance<T>;
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			doc.RootElement.TryGetProperty("head", out var head);
			var variables = JsonSerializer.Deserialize<string[]>(head, options);
			if (variables.ContentsEqual(["s", "p", "o"]) == false)
				throw new JsonException("Expected head to be ['s', 'p', 'o']");

			doc.RootElement.TryGetProperty("results", out var results);
			results.TryGetProperty("bindings", out var bindings);
			var graph = _supplier();
			foreach (var element in bindings.EnumerateArray())
			{
				var subject = ConverterUtils.ReadNode(element.GetProperty("s"));
				var predicate = ConverterUtils.ReadNode(element.GetProperty("p"));
				var obj = ConverterUtils.ReadNode(element.GetProperty("o"));
				graph.Assert(new Triple(subject, predicate, obj));
			}

			return graph;
		}



		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
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
				ConverterUtils.WriteNode(writer, triple.Subject);
				writer.WritePropertyName("p");
				ConverterUtils.WriteNode(writer, triple.Predicate);
				writer.WritePropertyName("o");
				ConverterUtils.WriteNode(writer, triple.Object);
				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.WriteEndObject();
		}
	}
}
