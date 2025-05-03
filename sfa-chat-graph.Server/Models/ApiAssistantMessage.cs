using MessagePack;
using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.Models
{
	[MessagePackObject]
	public class ApiAssistantMessage : ApiMessage
	{
		public ApiAssistantMessage() : base(ChatRole.Assistant, null)
		{
		}

		public ApiAssistantMessage(string content) : base(ChatRole.Assistant, content)
		{
		}

	}
}
