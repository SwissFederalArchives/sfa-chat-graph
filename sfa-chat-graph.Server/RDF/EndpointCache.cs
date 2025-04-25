using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace sfa_chat_graph.Server.RDF
{
	public class EndpointCache
	{
		[BsonId]
		[BsonElement("_id")]
		public ObjectId Id { get; set; }
	
		public string Endpoint { get; set; }
		public string[] Graphs { get; set; }
	}
}
