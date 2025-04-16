using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Utils.Bson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
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
