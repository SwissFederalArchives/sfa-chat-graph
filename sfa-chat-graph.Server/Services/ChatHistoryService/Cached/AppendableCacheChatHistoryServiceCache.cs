using MessagePack;
using Microsoft.AspNetCore.Mvc;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.Cache;
using sfa_chat_graph.Server.Utils.MessagePack;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.Cached
{
	public class AppendableCacheChatHistoryServiceCache : IChatHistoryServiceCache
	{
		private readonly IAppendableCache<Guid, IApiMessage> _cache;
		public bool SupportsToolData => false;

		public AppendableCacheChatHistoryServiceCache(IAppendableCache<Guid, IApiMessage> cache)
		{
			_cache=cache;
		}

		public Task<bool> ExistsAsync(Guid id) => _cache.ExistsAsync(id);

		public Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages) => _cache.AppendAsync(chatId, messages);

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id, bool loadBlobs = false)
		{
			var messages = await _cache.GetAsync(id).OfType<ApiMessage>().ToArrayAsync();
			return new ChatHistory
			{
				Id = id,
				Messages = messages
			};
		}

		public Task CacheHistoryAsync(ChatHistory history) => _cache.SetAsync(history.Id, history.Messages);

		public Task<FileResult> GetToolDataAsync(Guid toolDataId)
		{
			throw new NotImplementedException();
		}
	}
}
