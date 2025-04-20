using MessagePack;
using MessagePack.Resolvers;
using MongoDB.Driver;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.Cache;
using sfa_chat_graph.Server.Utils.MessagePack;
using StackExchange.Redis;
using System.Text.Json;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public class CachedChatHistoryService : IChatHistoryService
	{
		private readonly IChatHistoryService _storage;
		private readonly IChatHistoryServiceCache _cache;
		public bool SupportsToolData => _storage.SupportsToolData;

		public CachedChatHistoryService([FromKeyedServices("Storage")]IChatHistoryService storage, IChatHistoryServiceCache cache)
		{
			_storage = storage;
			_cache = cache;
		}


		public async Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			var existsTask = _cache.ExistsAsync(chatId);
			var storeTask = _storage.AppendAsync(chatId, messages);
			await Task.WhenAll(storeTask, existsTask);
			if (existsTask.Result)
			{
				var history = await _storage.GetChatHistoryAsync(chatId);
				await _cache.CacheHistoryAsync(history);
			}
			else
			{
				await _cache.AppendAsync(chatId, messages);
			}
		}

		public Task<bool> ExistsAsync(Guid id) => _storage.ExistsAsync(id);

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var isCached = await _cache.ExistsAsync(id);
			if (isCached)
				return await _cache.GetChatHistoryAsync(id);
		
			var res = await _storage.GetChatHistoryAsync(id);
			await _cache.CacheHistoryAsync(res);
			return res;
		}

		public Task<ApiToolData> GetToolDataAsync(Guid toolDataId) => _storage.GetToolDataAsync(toolDataId);
	}
}
