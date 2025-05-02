using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace sfa_chat_graph.Server.RDF
{
	public class EndpointCache
	{
		[BsonId(IdGenerator = typeof(BsonObjectIdGenerator))]
		[BsonElement("_id")]
		public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

		public string Endpoint { get; set; }
		public string[] Graphs { get; set; }
	}
}
