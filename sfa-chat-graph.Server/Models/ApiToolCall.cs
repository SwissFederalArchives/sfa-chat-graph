using System.Text.Json;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
{
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

		public string ToolId { get; set; }
		public string ToolCallId { get; set; }
		public JsonDocument Arguments { get; set; }
	}
}
