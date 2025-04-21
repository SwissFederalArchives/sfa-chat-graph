using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB.V2
{
	public class MongoToolData
	{
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Id { get; set; }
		public bool IsBase64Content { get; set; }
		public string Description { get; set; }
		public string MimeType { get; set; }

		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid? ContentId { get; set; }
		public string Content { get; set; }

		public static ApiToolData ToApi(MongoToolData data)
		{
			return new ApiToolData
			{
				Id = data.Id,
				IsBase64Content = data.IsBase64Content,
				Description = data.Description,
				MimeType = data.MimeType,
				Content = data.Content,
			};
		}

		public static MongoToolData FromApi(ApiToolData data)
		{
			return new MongoToolData
			{
				Id = data.Id,
				IsBase64Content = data.IsBase64Content,
				Description = data.Description,
				MimeType = data.MimeType,
				Content = data.Content,
			};
		}
	}
}
