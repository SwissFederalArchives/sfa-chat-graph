using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json;

namespace SfaChatGraph.Server.Utils.Bson
{
	public class JsonDocumentBsonConverter : IBsonSerializer<JsonDocument>
	{
		public Type ValueType => typeof(JsonDocument);

		private JsonDocument DeserializeImpl(BsonDeserializationContext ctx, BsonDeserializationArgs args)
		{
			var json = ctx.Reader.ReadString();
			return JsonDocument.Parse(json);
		}

		public JsonDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);
		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);


		private void SerializeImpl(BsonSerializationContext ctx, BsonSerializationArgs args, JsonDocument value)
		{
			ctx.Writer.WriteString(value.RootElement.ToString());
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonDocument value) => SerializeImpl(context, args, value);
		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => SerializeImpl(context, args, (JsonDocument)value);

	}
}
