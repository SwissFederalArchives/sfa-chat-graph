using OpenAI.Assistants;
using SfaChatGraph.Server.Models;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
{
	public class ApiMessage
	{

		public ChatRole Role { get; set; }
		public string Content { get; set; }
		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

		public ApiMessage() : this(ChatRole.User, null)
		{

		}

		public ApiMessage(ChatRole role, string content)
		{
			this.Role = role;
			this.Content = content;
		}

		public static ApiMessage UserMessage(string content) => new ApiMessage(ChatRole.User, content);
		public static ApiToolResponseMessage ToolResponse(string id, string content) => new ApiToolResponseMessage(id, content);

	}
}
