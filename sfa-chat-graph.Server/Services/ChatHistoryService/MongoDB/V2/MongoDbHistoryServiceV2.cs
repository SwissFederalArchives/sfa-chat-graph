using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Utils;
using SfaChatGraph.Server.Utils.MessagePack;
using SfaChatGraph.Server.Versioning;
using SfaChatGraph.Server.Versioning.Migrations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Services.ChatHistoryService.MongoDB.V2
{
	[ServiceVersion<IChatHistoryService>(2)]
	public class MongoDbHistoryServiceV2 : MongoDbHistoryServiceBase, IChatHistoryService, IPostMigration
	{
		const int Version = 2;
		private static readonly MessagePackSerializerOptions _serializerOptions = MessagePackSerializerOptions.Standard
			.WithCompression(MessagePackCompression.Lz4BlockArray)
			.WithResolver(FormatterResolver.Instance);

		private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<MongoChatMessageModel> _messages;
		private readonly GridFSBucket _dataBucket;

		public bool SupportsToolData => true;

		public MongoDbHistoryServiceV2(IMongoDatabase database)
		{
			_db=database;
			_messages = _db.GetCollectionVersion<MongoChatMessageModel>("messages", Version);
			_dataBucket = _db.GetBucketVersionAsync("message-data", Version, opts =>
			{
				opts.ChunkSizeBytes = 1024 * 1024 * 10;
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
					stream = new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, false);

				await _dataBucket.UploadFromStreamAsync(data.ContentId.ToString(), stream);
				data.Content = null;
			}
		}

		private async Task StoreCodeToolDataAsync(MongoCodeToolData data)
		{
			if (data?.ToolData != null)
			{
				var tasks = data.ToolData.Select(StoreToolDataAsync);
				await Task.WhenAll(tasks);
			}
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
			var mongoMessages = messages.Select(x => MongoChatMessageModel.FromApi(chatId, x)).ToArray();
			var tasks = mongoMessages.Where(x => x.HasData).Select(StoreLargeDataAsync).ToArray();
			await Task.WhenAll(tasks);
			var firstEx = tasks.FirstOrDefault(x => x.IsFaulted);
			if (firstEx != null)
				throw firstEx.Exception;

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
				Stream stream = await _dataBucket.OpenDownloadStreamByNameAsync(data.ContentId.ToString());
				if (data.IsBase64Content)
					stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read, false);

				using (var reader = new StreamReader(stream, leaveOpen: false))
				{
					data.Content = await reader.ReadToEndAsync();
					data.BlobLoaded = true;
				}
			}
		}

		private async Task LoadCodeToolDataAsync(MongoCodeToolData data)
		{
			if (data.ToolData == null) return;
			var tasks = data.ToolData.Select(LoadMongoToolDataAsync);
			await Task.WhenAll(tasks);
		}

		private async Task<SparqlResultSet> LoadResultSetAsync(Guid id)
		{
			using (var stream = await _dataBucket.OpenDownloadStreamByNameAsync(id.ToString()))
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

		private async Task LoadLargeDataAsync(MongoChatMessageModel dataModel, bool loadBlobs = false)
		{
			if (dataModel.CodeToolData != null && loadBlobs)
				await LoadCodeToolDataAsync(dataModel.CodeToolData);

			if (dataModel.GraphToolData != null)
				await LoadGraphToolDataAsync(dataModel.GraphToolData);
		}

		public async Task<ChatHistory> GetChatHistoryAsync(Guid id, bool loadBlobs = false)
		{
			var messages = await _messages.Find(x => x.HistoryId == id).SortBy(x => x.TimeStamp).ToListAsync();
			var tasks = messages.Where(x => x.HasData).Select(x => LoadLargeDataAsync(x, loadBlobs)).ToArray();
			await Task.WhenAll(tasks);
			var apiMessage = messages.Select(x => x.ToApi()).ToArray();
			return new ChatHistory { Id = id, Messages = apiMessage };
		}

		public async Task<FileResult> GetToolDataAsync(Guid toolDataId)
		{
			var message = await _messages
				.Find(x => x.CodeToolData != null && x.CodeToolData.ToolData.Any(y => y.Id == toolDataId))
				.FirstOrDefaultAsync();

			var toolData = message?.CodeToolData.ToolData.FirstOrDefault(x => x.Id == toolDataId);
			if (toolData == null)
				return null;

			if (toolData.ContentId.HasValue)
			{
				var stream = await _dataBucket.OpenDownloadStreamByNameAsync(toolData.ContentId.ToString());
				return new FileStreamResult(stream, toolData.MimeType);
			}
			else
			{
				var bytes = toolData.IsBase64Content ? Convert.FromBase64String(toolData.Content) : Encoding.UTF8.GetBytes(toolData.Content);
				return new FileContentResult(bytes, toolData.MimeType);
			}
		}


		public override Task<ChatHistory> GetChatHistoryAsync(Guid id) => GetChatHistoryAsync(id, true);

		public override async Task DeleteAsync()
		{
			await _db.DropCollectionVersionAsync("messages", Version);
			await _db.DropBucketVersionAsync("message-data", Version);
		}

		public override Task StoreAsync(ChatHistory history) => AppendAsync(history.Id, history.Messages);

		public override async Task<IEnumerable<Guid>> GetChatHistoryIdsAsync()
		{
			var ids = await _messages.Aggregate()
				.Group(x => x.HistoryId, x => x.Key)
				.ToListAsync();

			return ids;
		}

		public async Task RunPostMigrationAsync(MigrationReport report, CancellationToken token)
		{
			var filter = Builders<GridFSFileInfo>.Filter.Empty;
			var filemeta = await _dataBucket.Find(filter).ToListAsync();
			var fileIds = filemeta.ToDictionary(x => x.Filename, x => x.Id);
			var cursor = await _messages.Find(x => x.GraphToolData != null || x.CodeToolData != null).ToCursorAsync();
			while (token.IsCancellationRequested == false && await cursor.MoveNextAsync())
			{
				var messages = cursor.Current;
				foreach (var message in messages)
				{
					if (message.GraphToolData != null)
					{
						if (message.GraphToolData.VisualisationGraphId.HasValue)
							fileIds.Remove(message.GraphToolData.VisualisationGraphId.Value.ToString());

						if (message.GraphToolData.DataGraphId.HasValue)
							fileIds.Remove(message.GraphToolData.DataGraphId.Value.ToString());
					}

					if (message.CodeToolData != null && message.CodeToolData.ToolData != null)
					{
						message.CodeToolData.ToolData.ForEach(x =>
						{
							if (x.ContentId.HasValue)
								fileIds.Remove(x.ContentId.Value.ToString());
						});
					}
				}
			}

			var tasks = fileIds.Values.Select(x => _dataBucket.DeleteAsync(x, token)).ToArray();
			await Task.WhenAll(tasks);
		}
	}
}
