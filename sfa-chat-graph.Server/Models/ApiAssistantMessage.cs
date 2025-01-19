using SfaChatGraph.Server.Models;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
{
	public class ApiAssistantMessage : ApiMessage
	{
		[JsonConstructor]
		public ApiAssistantMessage() : base(ChatRole.Assistant, null)
		{
		}

		public ApiAssistantMessage(string content) : base(ChatRole.Assistant, content)
		{
		}

	}
}
