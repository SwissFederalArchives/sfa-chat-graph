using MessagePack;
using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.Models
{
	[MessagePackObject]
	public class ApiToolCallMessage : ApiMessage
	{
		[Key(5)]
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
