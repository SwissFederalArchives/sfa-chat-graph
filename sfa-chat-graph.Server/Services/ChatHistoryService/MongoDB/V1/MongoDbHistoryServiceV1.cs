using MessagePack;
using Microsoft.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Utils.MessagePack;
using SfaChatGraph.Server.Versioning;
using SfaChatGraph.Server.Versioning.Migrations;
using System.Text.Json;

namespace SfaChatGraph.Server.Services.ChatHistoryService.MongoDB.V1
{
	[ServiceVersion<IChatHistoryService>(Version)]
	[Obsolete("This class is deprecated. Use MongoDbHistoryServiceV2 instead.")]
	public class MongoDbHistoryServiceV1 : MongoDbHistoryServiceBase
	{
		const int Version = 1;
		private static readonly MessagePackSerializerOptions _serializerOptions = MessagePackSerializerOptions.Standard
			.WithCompression(MessagePackCompression.Lz4BlockArray)
			.WithResolver(FormatterResolver.Instance);

		private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<MongoChatMessageModel> _messages;
		private readonly GridFSBucket _dataBucket;

		public bool CanDelete => true;

		public MongoDbHistoryServiceV1(IMongoDatabase database)
		{
			_db=database;
			_messages = _db.GetCollection<MongoChatMessageModel>("messages");
			_dataBucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "message-data",
				ChunkSizeBytes = 1024*1024*10
			});
		}

		private async Task HandleDataUploadAsync(MongoChatMessageModel data)
		{
			if (data.HasData)
			{
				using (var stream = _streamManager.GetStream())
				{
					if (data.HasGraphData)
						await MessagePackSerializer.SerializeAsync(stream, data.GraphToolData, _serializerOptions);

					if (data.HasCodeData)
						await MessagePackSerializer.SerializeAsync(stream, data.CodeToolData, _serializerOptions);

					stream.Position = 0;
					await _dataBucket.UploadFromStreamAsync(ObjectId.GenerateNewId(), data.MessageId.ToString(), stream);
				}
			}
		}

		public async Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			var mongoMessages = messages.Select(x => MongoChatMessageModel.FromApi(chatId, x)).ToArray();
			await _messages.InsertManyAsync(mongoMessages);
			var tasks = mongoMessages.Where(x => x.HasData).Select(HandleDataUploadAsync).ToArray();
			await Task.WhenAll(tasks);
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			var filter = Builders<MongoChatMessageModel>.Filter.Eq(x => x.HistoryId, id);
			var count = await _messages.CountDocumentsAsync(filter);
			return count > 0;
		}


		private async Task LoadMessageData(MongoChatMessageModel dataModel)
		{
			if (dataModel.HasData)
			{
				using (var stream = await _dataBucket.OpenDownloadStreamByNameAsync(dataModel.MessageId.ToString()))
				{
					if (dataModel.HasGraphData)
						dataModel.GraphToolData = await MessagePackSerializer.DeserializeAsync<ApiGraphToolData>(stream, _serializerOptions);

					if (dataModel.HasCodeData)
						dataModel.CodeToolData = await MessagePackSerializer.DeserializeAsync<ApiCodeToolData>(stream, _serializerOptions);
				}
			}
		}

		public override async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var messages = await _messages.Find(x => x.HistoryId == id).SortBy(x => x.TimeStamp).ToListAsync();
			var tasks = messages.Where(x => x.HasData).Select(LoadMessageData).ToArray();
			await Task.WhenAll(tasks);
			var apiMessage = messages.Select(x => x.ToApi()).ToArray();
			return new ChatHistory { Id = id, Messages = apiMessage };
		}

		public override async Task<IEnumerable<Guid>> GetChatHistoryIdsAsync()
		{
			var res = await _messages.Aggregate()
				.Group(x => x.HistoryId, x => x.Key)
				.ToListAsync();

			return res;
		}

		public override async Task StoreAsync(ChatHistory history)
		{
			await AppendAsync(history.Id, history.Messages);
		}

		public override async Task DeleteAsync()
		{
			await _db.DropCollectionAsync("messages");
			await _db.DropBucketAsync("message-data");
		}
	}
}
