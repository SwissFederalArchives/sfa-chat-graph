using AwosFramework.Generators.FunctionCalling;
using OpenAI.Chat;
using sfa_chat_graph.Server.Services.ChatService;
using SfaChatGraph.Server.RDF.Models;
using System.ComponentModel;
using VDS.RDF;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.RDF
{
	public interface IGraphRag
	{
		[FunctionCall("list_graphs")]
		[Description("Lists all graphs in the current database")]
		public Task<string[]> ListGraphsAsync([Description("Whether to allow cached graphs, always allow caching if the user not explicitly asks for a refresh")] bool ignoreCached = false);

		[FunctionCall("get_schema")]
		[Description("Gets the schema of a graph")]
		public Task<string> GetSchemaAsync([Context] IChatActivity activities, [Description("The iri of the graph to load the schema of")]string graph, [Description("Whether to allow cached schemas, always allow caching if the user not explicitly asks for a refresh")]bool ignoreCached = false);

		[FunctionCall("query")]
		[Description("Queries the database in sparql")]
		public Task<SparqlResultSet> QueryAsync([Description("A well formed sparql query string")]string query);

		[FunctionCall("describe")]
		[Description("Describes a subject in the database")]
		public Task<IGraph> DescribeAsync([Description("The iri of the subject to describe")] string iri);

		public Task<SparqlResultSet> GetVisualisationResultAsync(SparqlResultSet results, string query);
	}
}
