using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using SfaChatGraph.Server.Models;

namespace SfaChatGraph.Server.Services.ChatHistoryService
{
	public class ChatHistory
	{
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Id { get; set; }
		public ApiMessage[] Messages { get; set; } 
	}
}
