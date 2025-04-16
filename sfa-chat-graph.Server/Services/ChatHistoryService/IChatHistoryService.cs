using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public interface IChatHistoryService
	{
		public Task<ChatHistory> GetChatHistoryAsync(Guid id);
		public Task AppendAsync(Guid chatId, ApiMessage[] messages);
	}
}
