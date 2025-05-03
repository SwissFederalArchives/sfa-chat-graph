using Microsoft.AspNetCore.Mvc;
using SfaChatGraph.Server.Models;

namespace SfaChatGraph.Server.Services.ChatHistoryService
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
