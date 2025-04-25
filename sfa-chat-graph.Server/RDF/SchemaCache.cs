using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace sfa_chat_graph.Server.RDF
{
	public class SchemaCache
	{
		[BsonId]
		[BsonElement("_id")]
		public ObjectId Id { get; set; }

		public string Endpoint { get; set; }
		public string Graph { get; set; }
		public string Schema { get; set; }
	}
}
