using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace sfa_chat_graph.Server.Models
{
	[MessagePackObject]
	public class ApiToolData
	{
		[Key(0)]
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Id { get; set; }

		[Key(1)]
		public bool IsBase64Content { get; set; }

		[Key(2)]
		public string Description { get; set; }

		[Key(3)]
		public string MimeType { get; set; }

		[Key(4)]
		public string Content { get; set; }
	}
}
