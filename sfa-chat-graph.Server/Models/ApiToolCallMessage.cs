using SfaChatGraph.Server.Models;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
{
	public class ApiToolCallMessage : ApiMessage
	{
		public ApiToolCall[] ToolCalls { get; set; }

		public ApiToolCallMessage() : base(ChatRole.ToolCall, null)
		{
		}

		public ApiToolCallMessage(IEnumerable<ApiToolCall> toolCalls) : base(ChatRole.ToolCall, null)
		{
			ToolCalls = toolCalls.ToArray();
		}



	}
}
