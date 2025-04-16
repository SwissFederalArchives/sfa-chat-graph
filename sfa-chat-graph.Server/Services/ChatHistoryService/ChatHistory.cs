using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public class ChatHistory
	{
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Id { get; set; }
		public ApiMessage[] Messages { get; set; } 
	}
}
