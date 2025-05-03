using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace SfaChatGraph.Server.RDF
{
	public class SchemaCache
	{
		[BsonId(IdGenerator = typeof(BsonObjectIdGenerator))]
		[BsonElement("_id")]
		public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

		public string Endpoint { get; set; }
		public string Graph { get; set; }
		public string Schema { get; set; }
	}
}
