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
		private readonly IAppendableCache<Guid, IApiMessage> _cache;
		private readonly IMongoDatabase _db;

		private static readonly MessagePackSerializerOptions _msgPackOptions = new MessagePackSerializerOptions(FormatterResolver.Instance)
			.WithCompression(MessagePackCompression.Lz4BlockArray);

		public CachedChatHistoryService(IMongoDatabase db, IAppendableCache<Guid, IApiMessage> cache)
		{
			_db=db;
			_cache=cache;
		}

		public async Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var hasHistory = (await histories.CountDocumentsAsync(x => x.Id == chatId)) > 0;
			if (hasHistory == false)
			{
				var history = new ChatHistory
				{
					Id = chatId,
					Messages = messages.ToArray()
				};

				await histories.InsertOneAsync(history);
			}
			else
			{
				var update = Builders<ChatHistory>.Update.PushEach(x => x.Messages, messages);
				await histories.UpdateOneAsync(x => x.Id == chatId, update);
			}

			var cached = await _cache.ExistsAsync(chatId);
			if (cached)
			{
				await _cache.AppendAsync(chatId, messages);
			}
			else
			{
				if (hasHistory) // if there was existing history, load everything to refresh cache
				{
					var history = await GetChatHistoryAsync(chatId);
					messages = history.Messages;
				}

				await _cache.SetAsync(chatId, messages);
			}
		}

		private async Task<ChatHistory?> FindDbHistoryAsync(Guid id)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var filter = Builders<ChatHistory>.Filter.Eq(x => x.Id, id);
			var history = await histories.Find(filter).FirstOrDefaultAsync();
			return history;
		}

		

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var exists = await _cache.ExistsAsync(id);
			if (exists)
			{
				var messages = await _cache.GetAsync(id).OfType<ApiMessage>().ToArrayAsync();
				return new ChatHistory { Id = id, Messages = messages };
			}
			else
			{
				var history = await FindDbHistoryAsync(id);
				if (history != null)
				{
					await _cache.SetAsync(id, history.Messages);
					return history;
				}

				return new ChatHistory { Id = id, Messages = Array.Empty<ApiMessage>() };
			}
		}

	}
}
