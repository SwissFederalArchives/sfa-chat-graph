using OpenAI.Chat;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public record Message(ChatMessage OpenAi, ApiMessage Api)
	{
		public Message(ChatMessage msg) : this(msg, msg.AsApiMessage())
		{

		}

		public static implicit operator Message(ChatMessage msg) => new(msg);
	}
}
