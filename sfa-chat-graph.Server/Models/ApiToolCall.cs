using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using SfaChatGraph.Server.Utils.Bson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.Models
{
	[MessagePackObject]
	public class ApiToolCall
	{
		public ApiToolCall()
		{
		}

		public ApiToolCall(string toolId, string toolCallId, JsonDocument arguments)
		{
			ToolId=toolId;
			ToolCallId=toolCallId;
			Arguments=arguments;
		}

		[Key(0)]
		public string ToolId { get; set; }

		[Key(1)]
		public string ToolCallId { get; set; }

		[Key(2)]
		[BsonSerializer(typeof(JsonDocumentBsonConverter))]
		public JsonDocument Arguments { get; set; }
	}
}
