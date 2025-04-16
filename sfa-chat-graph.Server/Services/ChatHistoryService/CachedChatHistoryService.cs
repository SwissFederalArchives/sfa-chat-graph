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

		public async Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var history = await FindDbHistoryAsync(chatId);
			if (history == null)
			{
				history = new ChatHistory
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

			
			await CacheAsync(chatId, messages);
		}

		private async Task<ChatHistory?> FindDbHistoryAsync(Guid id)
		{
			var histories = _db.GetCollection<ChatHistory>("chat-history");
			var filter = Builders<ChatHistory>.Filter.Eq(x => x.Id, id);
			var history = await histories.Find(filter).FirstOrDefaultAsync();
			return history;
		}

		private async Task CacheAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			using var stream = new MemoryStream();
			foreach (var message in messages)
				MessagePackSerializer.Serialize(stream, message, _msgPackOptions);

			stream.Position = 0;
			await _redis.StringAppendAsync(chatId.ToString(), RedisValue.CreateFrom(stream));
		}

		private async Task SetCacheAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			using var stream = new MemoryStream();
			foreach (var message in messages)
				MessagePackSerializer.Serialize<IApiMessage>(stream, message, _msgPackOptions);

			stream.Position = 0;
			await _redis.StringSetAsync(chatId.ToString(), RedisValue.CreateFrom(stream));
		}

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var data = await _redis.StringGetAsync(id.ToString());
			if (data.HasValue && data.IsNullOrEmpty == false)
			{
				var history = new List<IApiMessage>(16);
				ReadOnlyMemory<byte> memory = data;
				var reader = new MessagePackReader(memory);
				while (reader.End == false)
					history.Add(MessagePackSerializer.Deserialize<IApiMessage>(ref reader, _msgPackOptions));

				return new ChatHistory { Id = id, Messages = history.OfType<ApiMessage>().ToArray() };
			}
			else
			{
				var history = await FindDbHistoryAsync(id);
				if (history != null)
				{
					await SetCacheAsync(id, history.Messages);
					return history;
				}

				return new ChatHistory { Id = id, Messages = Array.Empty<ApiMessage>() };
			}
		}

	}
}
