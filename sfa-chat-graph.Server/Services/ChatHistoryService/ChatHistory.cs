using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public class ChatHistory
	{
		public Guid Id { get; set; }
		public ApiMessage[] Messages { get; set; } 
	}
}
