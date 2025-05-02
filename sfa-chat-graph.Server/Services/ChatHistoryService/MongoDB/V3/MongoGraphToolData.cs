using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Utils.Bson;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB.V3
{
	public class MongoGraphToolData
	{
		public string Query { get; set; }

		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid? DataGraphId { get; set; }

		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid? VisualisationGraphId { get; set; }

		[BsonSerializer(typeof(SparqlResultSetBsonConverter))]
		public SparqlResultSet DataGraph { get; set; }
		
		[BsonSerializer(typeof(SparqlResultSetBsonConverter))]
		public SparqlResultSet VisualisationGraph { get; set; }

		public ApiGraphToolData ToApi()
		{
			return new ApiGraphToolData
			{
				Query = Query,
				DataGraph = DataGraph,
				VisualisationGraph = VisualisationGraph
			};
		}

		public static MongoGraphToolData FromApi(ApiGraphToolData data)
		{
			if (data == null) return null;
			return new MongoGraphToolData
			{
				Query = data.Query,
				DataGraph = data.DataGraph,
				VisualisationGraph = data.VisualisationGraph
			};
		}
	}
}
