using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Utils.Bson;
using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
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
