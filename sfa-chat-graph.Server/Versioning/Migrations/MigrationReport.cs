using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SfaChatGraph.Server.Versioning.Migrations
{
	public class MigrationReport
	{
		[BsonId]
		[BsonElement("_id")]
		public ObjectId Id { get; set; }

		public bool WasCancelled { get; init; }
		public string ServiceType { get; init; }
		public int FromVersion { get; init; }
		public int ToVersion { get; init; }
		public DateTime MigrationStarted { get; init; } 
		public DateTime MigrationEnded { get; init; }
		public string[] Migrated { get; init; }
		public MigrationError[] Errors { get; init; }
		public bool Success { get; init; }

		[BsonIgnore]
		public TimeSpan Duration => MigrationEnded - MigrationStarted;
	}
}
