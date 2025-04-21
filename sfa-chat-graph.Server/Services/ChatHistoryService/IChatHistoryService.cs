using Microsoft.AspNetCore.Mvc;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public interface IChatHistoryService
	{
		public Task<bool> ExistsAsync(Guid id);
		public Task<ChatHistory> GetChatHistoryAsync(Guid id, bool loadBlobs = false);
		public Task AppendAsync(Guid chatId, params ApiMessage[] messages) => AppendAsync(chatId, (IEnumerable<ApiMessage>)messages);
		public Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages);
		public bool SupportsToolData { get; }
		public Task<FileResult> GetToolDataAsync(Guid toolDataId);
	}
}
