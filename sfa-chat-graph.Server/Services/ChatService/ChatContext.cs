using OpenAI.Chat;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatService
{
	public class ChatContext
	{
		public Guid ChatId { get; init; }
		public List<ApiMessage> Created { get; init; } = new();
		public ApiMessage[] History { get; init; }
		

		public IEnumerable<ApiToolResponseMessage> ToolResponses => History.OfType<ApiToolResponseMessage>().Concat(Created.OfType<ApiToolResponseMessage>());

		public ChatContext(IEnumerable<ApiMessage> history)
		{
			this.History = history.ToArray();
			this.ChatId = Guid.NewGuid();
		}
	}
}
