using MessagePack;
using MessagePack.Resolvers;
using MongoDB.Driver;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Utils.MessagePack;
using StackExchange.Redis;
using System.Text.Json;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public class CachedChatHistoryService : IChatHistoryService
	{
		private readonly IDatabaseAsync _redis;
		private readonly IMongoDatabase _db;

		private static readonly MessagePackSerializerOptions _msgPackOptions = new MessagePackSerializerOptions(FormatterResolver.Instance)
			.WithCompression(MessagePackCompression.Lz4BlockArray);

		public CachedChatHistoryService(IDatabaseAsync redis, IMongoDatabase db)
		{
			_redis=redis;
			_db=db;
		}

		public async Task AppendAsync(Guid chatId, ApiMessage[] messages)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var history = await FindDbHistoryAsync(chatId);
			Task dbTask;
			if (history == null)
			{
				history = new ChatHistory
				{
					Id = chatId,
					Messages = messages
				};

				dbTask = histories.InsertOneAsync(history);
			}
			else
			{
				var update = Builders<ChatHistory>.Update.PushEach(x => x.Messages, messages);
				dbTask = histories.UpdateOneAsync(x => x.Id == chatId, update);
			}

			var cacheTask = CacheAsync(chatId, messages);
			await Task.WhenAll(dbTask, cacheTask);
		}

		private async Task<ChatHistory?> FindDbHistoryAsync(Guid id)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var filter = Builders<ChatHistory>.Filter.Eq(x => x.Id, id);
			var history = await histories.Find(filter).FirstOrDefaultAsync();
			return history;
		}

		private async Task CacheAsync(Guid chatId, ApiMessage[] messages)
		{
			using var stream = new MemoryStream();
			foreach (var message in messages)
				MessagePackSerializer.Serialize(stream, message);

			await _redis.StringAppendAsync(chatId.ToString(), stream.ToArray());
		}

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var data = await _redis.StringGetAsync(id.ToString());
			if (data.HasValue && data.IsNullOrEmpty == false)
			{
				var history = new List<ApiMessage>(16);
				ReadOnlyMemory<byte> memory = data;
				var reader = new MessagePackReader(memory);
				while (reader.End == false)
					history.Add(MessagePackSerializer.Deserialize<ApiMessage>(ref reader, _msgPackOptions));

				return new ChatHistory { Id = id, Messages = history.ToArray() };
			}
			else
			{
				var history = await FindDbHistoryAsync(id);
				if (history != null)
				{
					await CacheAsync(id, history.Messages);
					return history;
				}

				return new ChatHistory { Id = id, Messages = Array.Empty<ApiMessage>() };
			}
		}

	}
}
