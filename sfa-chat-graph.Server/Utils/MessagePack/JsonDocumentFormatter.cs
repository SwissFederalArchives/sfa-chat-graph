using MessagePack;
using MessagePack.Formatters;
using System.Text.Json;

namespace sfa_chat_graph.Server.Utils.MessagePack
{
	public class JsonDocumentFormatter : IMessagePackFormatter<JsonDocument>
	{
		private delegate ReadOnlyMemory<byte> GetRawValue(JsonDocument doc, int indec, bool includeQoutes);
		private static GetRawValue GetRawValueJD = typeof(JsonDocument).GetMethod("GetRawValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			.CreateDelegate<GetRawValue>();

		public JsonDocument Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			var data = reader.ReadBytes();
			if (data.HasValue == false)
				return null;

			var jsonReader = new Utf8JsonReader(data.Value);
			return JsonSerializer.Deserialize<JsonDocument>(ref jsonReader);
		}

		public void Serialize(ref MessagePackWriter writer, JsonDocument value, MessagePackSerializerOptions options)
		{
			var memory = GetRawValueJD(value, 0, false);
			writer.WriteBinHeader(memory.Length);
			writer.WriteRaw(memory.Span);
		}
	}
}
