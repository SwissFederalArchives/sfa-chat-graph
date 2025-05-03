using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using SfaChatGraph.Server.Utils.Bson;
using VDS.RDF;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Models
{
	[MessagePackObject]
	public class ApiGraphToolData
	{
		[Key(0)]
		public string Query { get; set; }

		[Key(1)]
		[BsonSerializer(typeof(SparqlResultSetBsonConverter))]
		public SparqlResultSet VisualisationGraph { get; set; }

		[Key(2)]
		[BsonSerializer(typeof(SparqlResultSetBsonConverter))]
		public SparqlResultSet DataGraph { get; set; }
	}
}
