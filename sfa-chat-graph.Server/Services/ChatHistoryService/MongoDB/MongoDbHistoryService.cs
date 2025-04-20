using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Utils.MessagePack;
using SfaChatGraph.Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB
{
	public class MongoDbHistoryService : IChatHistoryService
	{
		private static readonly MessagePackSerializerOptions _serializerOptions = MessagePackSerializerOptions.Standard
			.WithCompression(MessagePackCompression.Lz4BlockArray)
			.WithResolver(FormatterResolver.Instance);

		private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<MongoChatMessageModel> _messages;
		private readonly GridFSBucket _dataBucket;

		public bool SupportsToolData => true;

		public MongoDbHistoryService(IMongoDatabase database)
		{
			_db=database;
			_messages = _db.GetCollection<MongoChatMessageModel>("messages");
			_dataBucket = new GridFSBucket(_db, new GridFSBucketOptions
			{
				BucketName = "message-data",
				ChunkSizeBytes = 1024 * 1024 * 10,
			});
		}


		private async Task StoreSparqlResultAsync(Guid id, SparqlResultSet set)
		{
			using (var stream = _streamManager.GetStream())
			{
				await MessagePackSerializer.SerializeAsync(stream, set, _serializerOptions);
				stream.Position = 0;
				await _dataBucket.UploadFromStreamAsync(id.ToString(), stream);
			}
		}

		private async Task StoreGraphToolDataAsync(MongoGraphToolData data)
		{
			if (data.DataGraph != null && data.DataGraph.Count > 10)
			{
				data.DataGraphId = Guid.NewGuid();
				await StoreSparqlResultAsync(data.DataGraphId.Value, data.DataGraph);
				data.DataGraph = null;
			}

			if (data.VisualisationGraph != null && data.VisualisationGraph.Count > 10)
			{
				data.VisualisationGraphId = Guid.NewGuid();
				await StoreSparqlResultAsync(data.VisualisationGraphId.Value, data.VisualisationGraph);
				data.VisualisationGraph = null;
			}
		}

		private async Task StoreToolDataAsync(MongoToolData data)
		{
			if (data.Content.Length > 512)
			{
				data.ContentId = Guid.NewGuid();
				Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data.Content));
				if (data.IsBase64Content)
					stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read, false);

				await _dataBucket.UploadFromStreamAsync(data.ContentId.ToString(), stream);
				data.Content = null;
			}
		}

		private async Task StoreCodeToolDataAsync(MongoCodeToolData data)
		{
			var tasks = data.ToolData.Select(StoreToolDataAsync);
			await Task.WhenAll(tasks);
		}

		private async Task StoreLargeDataAsync(MongoChatMessageModel data)
		{
			if (data.CodeToolData != null)
				await StoreCodeToolDataAsync(data.CodeToolData);

			if (data.GraphToolData != null)
				await StoreGraphToolDataAsync(data.GraphToolData);
		}

		public async Task AppendAsync(Guid chatId, IEnumerable<ApiMessage> messages)
		{
			var mongoMessages = messages.Select(x => MongoChatMessageModel.FromApi(chatId, x));
			var tasks = mongoMessages.Where(x => x.HasData).Select(StoreLargeDataAsync);
			await Task.WhenAll(tasks);
			await _messages.InsertManyAsync(mongoMessages);
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			var filter = Builders<MongoChatMessageModel>.Filter.Eq(x => x.HistoryId, id);
			var count = await _messages.CountDocumentsAsync(filter);
			return count > 0;
		}

		private async Task LoadMongoToolDataAsync(MongoToolData data)
		{
			if (data.ContentId != null)
			{
				Stream stream = await _dataBucket.OpenDownloadStreamAsync(data.ContentId.ToString());
				if (data.IsBase64Content)
					stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read, false);

				using (var reader = new StreamReader(stream, leaveOpen: false))
					data.Content = await reader.ReadToEndAsync();
			}
		}

		private async Task LoadCodeToolDataAsync(MongoCodeToolData data)
		{
			var tasks = data.ToolData.Select(LoadMongoToolDataAsync);
			await Task.WhenAll(tasks);
		}

		private async Task<SparqlResultSet> LoadResultSetAsync(Guid id)
		{
			using (var stream = await _dataBucket.OpenDownloadStreamAsync(id.ToString()))
			{
				return await MessagePackSerializer.DeserializeAsync<SparqlResultSet>(stream, _serializerOptions);
			}
		}

		private async Task LoadGraphToolDataAsync(MongoGraphToolData data)
		{
			if (data.VisualisationGraphId.HasValue)
				data.VisualisationGraph = await LoadResultSetAsync(data.VisualisationGraphId.Value);

			if (data.DataGraphId.HasValue)
				data.DataGraph = await LoadResultSetAsync(data.DataGraphId.Value);
		}

		private async Task LoadLargeDataAsync(MongoChatMessageModel dataModel)
		{
			if (dataModel.CodeToolData != null)
				await LoadCodeToolDataAsync(dataModel.CodeToolData);

			if (dataModel.GraphToolData != null)
				await LoadGraphToolDataAsync(dataModel.GraphToolData);
		}

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id)
		{
			var messages = await _messages.Find(x => x.HistoryId == id).SortBy(x => x.TimeStamp).ToListAsync();
			var tasks = messages.Where(x => x.HasData).Select(LoadLargeDataAsync);
			await Task.WhenAll(tasks);
			var apiMessage = messages.Select(x => x.ToApi).ToArray();
			return new ChatHistory { Id = id, Messages = apiMessage };
		}

		public async Task<FileResult> GetToolDataAsync(Guid toolDataId)
		{
			var toolData = await _messages.Aggregate()
					.Match(x => x.CodeToolData != null)
					.Unwind<MongoChatMessageModel, MongoToolData>(x => x.CodeToolData.ToolData)
					.Match(x => x.Id == toolDataId)
					.FirstOrDefaultAsync();

			if (toolData == null)
				return null;

			if (toolData.ContentId.HasValue)
			{
				var stream = await _dataBucket.OpenDownloadStreamByNameAsync(toolDataId.ToString());
				return new FileStreamResult(stream, toolData.MimeType);
			
			}
			else
			{
				var bytes = toolData.IsBase64Content ? Convert.FromBase64String(toolData.Content) : Encoding.UTF8.GetBytes(toolData.Content);
				return new FileContentResult(bytes, toolData.MimeType);
			}
		}
	}
}
